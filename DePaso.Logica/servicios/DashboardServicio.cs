using DePaso.Entidades;
using DePaso.Logica.Dtos;
using DePaso.Logica.enums;
using DePaso.Logica.helpers;
using Microsoft.EntityFrameworkCore;

using Ocurrencia = DePaso.Logica.helpers.ReglasCoincidencia.Ocurrencia;

namespace DePaso.Logica.servicios
{
    public class DashboardServicio : IDashboardServicio
    {
        public const int MAX_ITEMS_LISTA = 5;
        public const int DIAS_VENTANA = 31;

        private readonly DePasoAppContext _context;
        private readonly IEstimadorDesvio _estimador;
        private readonly IOptimizadorCombinaciones _optimizador;
        private readonly TimeProvider _reloj;

        public DashboardServicio(
            DePasoAppContext context,
            IEstimadorDesvio estimador,
            IOptimizadorCombinaciones optimizador,
            TimeProvider? reloj = null
        )
        {
            _context = context;
            _estimador = estimador;
            _optimizador = optimizador;
            _reloj = reloj ?? TimeProvider.System;
        }

        public async Task<DashboardResumenDto?> ObtenerResumenAsync(
            int usuarioId,
            CancellationToken cancelacion = default
        )
        {
            var usuario = await _context
                .Usuarios.AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => new { u.Nombre })
                .FirstOrDefaultAsync(cancelacion);
            if (usuario == null)
            {
                return null;
            }

            var ahora = _reloj.GetLocalNow().DateTime;
            var hoy = DateOnly.FromDateTime(ahora);
            var horaActual = TimeOnly.FromDateTime(ahora);

            var viajes = await _context
                .Viajes.AsNoTracking()
                .Include(v => v.ViajeDia)
                .Include(v => v.Paradas)
                .Where(v => v.UsuarioId == usuarioId && v.Activo == true)
                .ToListAsync(cancelacion);

            var ocurrencias = ReglasCoincidencia.GenerarOcurrencias(viajes, hoy, horaActual, DIAS_VENTANA);

            var proximos = ocurrencias.GroupBy(o => o.Viaje.Id).Select(g => g.First()).ToList();

            var enviosPendientes = await ObtenerEnviosPendientesAsync(usuarioId, cancelacion);

            var oportunidades = 0;
            var potencial = 0m;
            if (ocurrencias.Count > 0)
            {
                var resultado = await CalcularOportunidadesYPotencialAsync(
                usuarioId,
                viajes,
                ocurrencias,
                hoy,
                cancelacion
            );

                oportunidades = resultado.Oportunidades;
                potencial = resultado.Detalle.GananciaTotal;
            }

            return new DashboardResumenDto(
                NombreUsuario: usuario.Nombre,
                ProximosViajes: proximos.Count,
                EnviosPendientes: enviosPendientes.Count,
                Oportunidades: oportunidades,
                PotencialMes: potencial,
                ListaProximosViajes: proximos.Take(MAX_ITEMS_LISTA).Select(AViajeDto).ToList(),
                ListaEnviosPendientes: enviosPendientes
                    .Take(MAX_ITEMS_LISTA)
                    .Select(AEnvioDto)
                    .ToList()
            );
        }
        public async Task<PotencialMesDto?> ObtenerPotencialMesAsync(
    int usuarioId,
    string criterio,
    CancellationToken cancelacion = default)
{
    var existeUsuario = await _context.Usuarios
        .AnyAsync(u => u.Id == usuarioId, cancelacion);

    if (!existeUsuario)
    {
        return null;
    }

    criterio = criterio?.Trim().ToLowerInvariant() ?? "ganancia";

    if (criterio != "ganancia" &&
        criterio != "desvio" &&
        criterio != "equilibrio")
    {
        criterio = "ganancia";
    }

    var ahora = _reloj.GetLocalNow().DateTime;
    var hoy = DateOnly.FromDateTime(ahora);
    var horaActual = TimeOnly.FromDateTime(ahora);

    var viajes = await _context.Viajes
        .AsNoTracking()
        .Include(v => v.ViajeDia)
        .Include(v => v.Paradas)
        .Where(v => v.UsuarioId == usuarioId && v.Activo == true)
        .ToListAsync(cancelacion);

    var ocurrencias = ReglasCoincidencia.GenerarOcurrencias(
        viajes,
        hoy,
        horaActual,
        DIAS_VENTANA
    );

    if (ocurrencias.Count == 0)
    {
        return new PotencialMesDto(
            criterio,
            0m,
            0m,
            0,
            new List<PotencialSemanaDto>(),
            null
        );
    }

    var resultado = await CalcularOportunidadesYPotencialAsync(
        usuarioId,
        viajes,
        ocurrencias,
        hoy,
        cancelacion,
        criterio
    );

    return resultado.Detalle;
}
        private async Task<List<Envios>> ObtenerEnviosPendientesAsync(
            int usuarioId,
            CancellationToken cancelacion
        )
        {
            var estados = new[]
            {
                (byte)EstadoEnvio.BuscandoConductor,
                (byte)EstadoEnvio.ConductorAsignado,
            };
            return await _context
                .Envios.AsNoTracking()
                .Where(e => e.UsuarioId == usuarioId && estados.Contains(e.Estado))
                .OrderBy(e => e.FechaLimite)
                .ThenBy(e => e.Id)
                .ToListAsync(cancelacion);
        }

        private async Task<(
            int Oportunidades,
            PotencialMesDto Detalle
        )> CalcularOportunidadesYPotencialAsync(
            int usuarioId,
            IReadOnlyList<Viajes> viajes,
            IReadOnlyList<Ocurrencia> ocurrencias,
            DateOnly hoy,
            CancellationToken cancelacion,
            string criterio = "ganancia"
        )
        {
            var buscando = (byte)EstadoEnvio.BuscandoConductor;
            var pesoMaximo = viajes.Max(v => v.PesoMaxKg);

            var candidatos = await _context
                .Envios.AsNoTracking()
                .Where(e =>
                    e.UsuarioId != usuarioId
                    && e.Estado == buscando
                    && e.FechaLimite >= hoy
                    && e.PesoKg <= pesoMaximo
                    && !_context.Asignaciones.Any(a => a.EnvioId == e.Id)
                )
                .ToListAsync(cancelacion);
            if (candidatos.Count == 0)
            {
                return (0, new PotencialMesDto(
                    criterio,
                    0m,
                    0m,
                    0,
                    new List<PotencialSemanaDto>(),
                    null
                ));
            }

            var viajeIds = viajes.Select(v => v.Id).ToList();
            var ocupacion = await _context
                .Asignaciones.AsNoTracking()
                .Where(a => viajeIds.Contains(a.ViajeId) && a.FechaViaje >= hoy)
                .Select(a => new ReglasCoincidencia.OcupacionSalida(
                    a.ViajeId,
                    a.FechaViaje,
                    a.Envio.PesoKg,
                    a.DesvioKm
                ))
                .ToListAsync(cancelacion);

            var rutas = viajes.ToDictionary(v => v.Id, ReglasCoincidencia.ConstruirRuta);
            var desvios = new Dictionary<(int ViajeId, int EnvioId), decimal>();

            decimal DesvioDe(Viajes viaje, Envios envio)
            {
                if (!desvios.TryGetValue((viaje.Id, envio.Id), out var desvio))
                {
                    desvio = ReglasCoincidencia.Desvio(_estimador, rutas[viaje.Id], envio);
                    desvios[(viaje.Id, envio.Id)] = desvio;
                }

                return desvio;
            }

            LimitesViaje Restantes(Ocurrencia o) =>
                ReglasCoincidencia.Restantes(o.Viaje, o.Fecha, ocupacion);

            List<CandidatoEnvio> Elegibles(
                Ocurrencia o,
                LimitesViaje limites,
                HashSet<int>? excluidos
            )
            {
                if (limites.MaxPaquetes <= 0)
                {
                    return new List<CandidatoEnvio>();
                }

                return ReglasCoincidencia
                    .Evaluar(
                        candidatos.Where(e =>
                            e.FechaLimite >= o.Fecha
                            && (excluidos == null || !excluidos.Contains(e.Id))
                        ),
                        limites,
                        e => DesvioDe(o.Viaje, e)
                    )
                    .Where(x => x.Entra)
                    .Select(x => new CandidatoEnvio(x.Envio.Id, x.Envio.Monto, x.Envio.PesoKg, x.DesvioKm))
                    .ToList();
            }

            var oportunidades = new HashSet<int>();
            foreach (var o in ocurrencias)
            {
                foreach (var c in Elegibles(o, Restantes(o), null))
                {
                    oportunidades.Add(c.EnvioId);
                }
            }

            var usados = new HashSet<int>();

            var detalle = new List<(
                Ocurrencia Salida,
                CombinacionOptima Combinacion
            )>();

            var delMes = ocurrencias.Where(o =>
                o.Fecha.Year == hoy.Year &&
                o.Fecha.Month == hoy.Month
            );

            foreach (var o in delMes)
            {
                var limites = Restantes(o);

                var combinacion = OptimizadorPotencial.Optimizar(
                    Elegibles(o, limites, usados),
                    limites,
                    criterio,
                    _optimizador
                );

                detalle.Add((o, combinacion));

                foreach (var c in combinacion.Envios)
                {
                    usados.Add(c.EnvioId);
                }
            }

            var ultimoDia = new DateOnly(
                hoy.Year,
                hoy.Month,
                DateTime.DaysInMonth(hoy.Year, hoy.Month)
            );

            var semanas = detalle
                .GroupBy(d => (d.Salida.Fecha.Day - 1) / 7)
                .OrderBy(g => g.Key)
                .Select(g => new PotencialSemanaDto(
                    new DateOnly(hoy.Year, hoy.Month, g.Key * 7 + 1),
                    new DateOnly(
                        hoy.Year,
                        hoy.Month,
                        Math.Min(g.Key * 7 + 7, ultimoDia.Day)
                    ),
                    g.Sum(d => d.Combinacion.Ganancia),
                    g.Sum(d => d.Combinacion.DesvioKm),
                    g.Sum(d => d.Combinacion.Cantidad)
                ))
                .ToList();

            var mejorRecorrido = detalle
            .Where(d => d.Combinacion.Cantidad > 0)
            .GroupBy(d => d.Salida.Viaje.Id)
            .Select(g =>
            {
                var viaje = g.First().Salida.Viaje;

                return new PotencialRecorridoDto(
                    g.Key,
                    $"{viaje.OrigenCalle} {viaje.OrigenAltura}, {viaje.OrigenLocalidad}, {viaje.OrigenProvincia}",
                    $"{viaje.DestinoCalle} {viaje.DestinoAltura}, {viaje.DestinoLocalidad}, {viaje.DestinoProvincia}",
                    g.Sum(d => d.Combinacion.Ganancia)
                );
            })
            .OrderByDescending(r => r.Ganancia)
            .FirstOrDefault();

            var resultado = new PotencialMesDto(
                criterio,
                detalle.Sum(d => d.Combinacion.Ganancia),
                detalle.Sum(d => d.Combinacion.DesvioKm),
                usados.Count,
                semanas,
                mejorRecorrido
            );

            return (oportunidades.Count, resultado);
        }

        private static ViajeProximoDto AViajeDto(Ocurrencia o) =>
            new(
                o.Viaje.Id,
                $"{o.Viaje.OrigenCalle} {o.Viaje.OrigenAltura}, {o.Viaje.OrigenLocalidad}, {o.Viaje.OrigenProvincia}",
                $"{o.Viaje.DestinoCalle} {o.Viaje.DestinoAltura}, {o.Viaje.DestinoLocalidad}, {o.Viaje.DestinoProvincia}",
                DiasSemana.Formatear(o.Viaje.ViajeDia.Select(d => d.DiaSemana)),
                o.Viaje.HoraSalida,
                o.Viaje.HoraLlegada,
                o.Fecha
            );

        private static EnvioPendienteDto AEnvioDto(Envios e) =>
            new(
                e.Id,
                $"{e.OrigenCalle} {e.OrigenAltura}, {e.OrigenLocalidad}, {e.OrigenProvincia}",
                $"{e.DestinoCalle} {e.DestinoAltura}, {e.DestinoLocalidad}, {e.DestinoProvincia}",
                e.Descripcion,
                e.Monto,
                (EstadoEnvio)e.Estado,
                EstadosEnvio.Texto(e.Estado),
                e.FechaLimite
            );

    }
}

using DePaso.Entidades;
using DePaso.Logica.Dtos;
using DePaso.Logica.enums;
using DePaso.Logica.helpers;
using Microsoft.EntityFrameworkCore;

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
            TimeProvider? reloj = null)
        {
            _context = context;
            _estimador = estimador;
            _optimizador = optimizador;
            _reloj = reloj ?? TimeProvider.System;
        }

        public async Task<DashboardResumenDto?> ObtenerResumenAsync(int usuarioId, CancellationToken cancelacion = default)
        {
            var usuario = await _context.Usuarios.AsNoTracking()
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

            var viajes = await _context.Viajes.AsNoTracking()
                .Include(v => v.ViajeDia)
                .Include(v => v.Paradas)
                .Where(v => v.UsuarioId == usuarioId && v.Activo == true)
                .ToListAsync(cancelacion);

            var ocurrencias = GenerarOcurrencias(viajes, hoy, horaActual);

            var proximos = ocurrencias
                .GroupBy(o => o.Viaje.Id)
                .Select(g => g.First())
                .ToList();

            var enviosPendientes = await ObtenerEnviosPendientesAsync(usuarioId, cancelacion);

            var oportunidades = 0;
            var potencial = 0m;
            if (ocurrencias.Count > 0)
            {
                (oportunidades, potencial) = await CalcularOportunidadesYPotencialAsync(
                    usuarioId, viajes, ocurrencias, hoy, cancelacion);
            }

            return new DashboardResumenDto(
                NombreUsuario: usuario.Nombre,
                ProximosViajes: proximos.Count,
                EnviosPendientes: enviosPendientes.Count,
                Oportunidades: oportunidades,
                PotencialMes: potencial,
                ListaProximosViajes: proximos.Take(MAX_ITEMS_LISTA).Select(AViajeDto).ToList(),
                ListaEnviosPendientes: enviosPendientes.Take(MAX_ITEMS_LISTA).Select(AEnvioDto).ToList());
        }

        private async Task<List<Envios>> ObtenerEnviosPendientesAsync(int usuarioId, CancellationToken cancelacion)
        {
            var estados = new[] { (byte)EstadoEnvio.BuscandoConductor, (byte)EstadoEnvio.ConductorAsignado };
            return await _context.Envios.AsNoTracking()
                .Where(e => e.UsuarioId == usuarioId && estados.Contains(e.Estado))
                .OrderBy(e => e.FechaLimite)
                .ThenBy(e => e.Id)
                .ToListAsync(cancelacion);
        }

        private async Task<(int Oportunidades, decimal Potencial)> CalcularOportunidadesYPotencialAsync(
            int usuarioId,
            IReadOnlyList<Viajes> viajes,
            IReadOnlyList<Ocurrencia> ocurrencias,
            DateOnly hoy,
            CancellationToken cancelacion)
        {
            var buscando = (byte)EstadoEnvio.BuscandoConductor;
            var pesoMaximo = viajes.Max(v => v.PesoMaxKg);

            var candidatos = await _context.Envios.AsNoTracking()
                .Where(e => e.UsuarioId != usuarioId
                            && e.Estado == buscando
                            && e.FechaLimite >= hoy
                            && e.PesoKg <= pesoMaximo
                            && !_context.Asignaciones.Any(a => a.EnvioId == e.Id))
                .ToListAsync(cancelacion);
            if (candidatos.Count == 0)
            {
                return (0, 0m);
            }

            var viajeIds = viajes.Select(v => v.Id).ToList();
            var ocupacion = await _context.Asignaciones.AsNoTracking()
                .Where(a => viajeIds.Contains(a.ViajeId) && a.FechaViaje >= hoy)
                .Select(a => new { a.ViajeId, a.FechaViaje, a.DesvioKm, PesoKg = a.Envio.PesoKg })
                .ToListAsync(cancelacion);

            var rutas = viajes.ToDictionary(v => v.Id, ConstruirRuta);
            var desvios = new Dictionary<(int ViajeId, int EnvioId), decimal>();

            decimal DesvioDe(Viajes viaje, Envios envio)
            {
                if (!desvios.TryGetValue((viaje.Id, envio.Id), out var desvio))
                {
                    var km = _estimador.CalcularDesvioKm(
                        rutas[viaje.Id],
                        new Coordenada((double)envio.OrigenLat, (double)envio.OrigenLng),
                        new Coordenada((double)envio.DestinoLat, (double)envio.DestinoLng));
                    desvio = decimal.Round((decimal)km, 2);
                    desvios[(viaje.Id, envio.Id)] = desvio;
                }

                return desvio;
            }

            // Capacidad que queda en esa salida después de las asignaciones ya confirmadas.
            LimitesViaje Restantes(Ocurrencia o)
            {
                var asignadas = ocupacion.Where(a => a.ViajeId == o.Viaje.Id && a.FechaViaje == o.Fecha).ToList();
                return new LimitesViaje(
                    o.Viaje.MaxPaquetes - asignadas.Count,
                    o.Viaje.PesoMaxKg - asignadas.Sum(a => a.PesoKg),
                    o.Viaje.DesvioMaxKm - asignadas.Sum(a => a.DesvioKm));
            }

            List<CandidatoEnvio> Elegibles(Ocurrencia o, LimitesViaje limites, HashSet<int>? excluidos)
            {
                if (limites.MaxPaquetes <= 0)
                {
                    return new List<CandidatoEnvio>();
                }

                return candidatos
                    .Where(e => e.FechaLimite >= o.Fecha
                                && e.PesoKg <= limites.PesoMaxKg
                                && (excluidos == null || !excluidos.Contains(e.Id)))
                    .Select(e => new CandidatoEnvio(e.Id, e.Monto, e.PesoKg, DesvioDe(o.Viaje, e)))
                    .Where(c => c.DesvioKm <= limites.DesvioMaxKm)
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

            var potencial = 0m;
            var usados = new HashSet<int>();
            var delMes = ocurrencias.Where(o => o.Fecha.Year == hoy.Year && o.Fecha.Month == hoy.Month);
            foreach (var o in delMes)
            {
                var limites = Restantes(o);
                var combinacion = _optimizador.Optimizar(Elegibles(o, limites, usados), limites);
                potencial += combinacion.Ganancia;
                foreach (var c in combinacion.Envios)
                {
                    usados.Add(c.EnvioId);
                }
            }

            return (oportunidades.Count, potencial);
        }

        private static List<Coordenada> ConstruirRuta(Viajes viaje)
        {
            var puntos = new List<Coordenada> { new((double)viaje.OrigenLat, (double)viaje.OrigenLng) };
            puntos.AddRange(viaje.Paradas
                .OrderBy(p => p.Orden)
                .Select(p => new Coordenada((double)p.Lat, (double)p.Lng)));
            puntos.Add(new Coordenada((double)viaje.DestinoLat, (double)viaje.DestinoLng));
            return puntos;
        }

        private static List<Ocurrencia> GenerarOcurrencias(IEnumerable<Viajes> viajes, DateOnly hoy, TimeOnly horaActual)
        {
            var resultado = new List<Ocurrencia>();
            foreach (var viaje in viajes)
            {
                var dias = viaje.ViajeDia.Select(d => d.DiaSemana).ToHashSet();
                for (var i = 0; i < DIAS_VENTANA; i++)
                {
                    var fecha = hoy.AddDays(i);
                    if (!dias.Contains(DiasSemana.Numero(fecha.DayOfWeek)))
                    {
                        continue;
                    }

                    if (i == 0 && viaje.HoraSalida <= horaActual)
                    {
                        continue;
                    }

                    resultado.Add(new Ocurrencia(viaje, fecha));
                }
            }

            return resultado
                .OrderBy(o => o.Fecha)
                .ThenBy(o => o.Viaje.HoraSalida)
                .ThenBy(o => o.Viaje.Id)
                .ToList();
        }

        private static ViajeProximoDto AViajeDto(Ocurrencia o) => new(
            o.Viaje.Id,
            o.Viaje.OrigenDir,
            o.Viaje.DestinoDir,
            DiasSemana.Formatear(o.Viaje.ViajeDia.Select(d => d.DiaSemana)),
            o.Viaje.HoraSalida,
            o.Viaje.HoraLlegada,
            o.Fecha);

        private static EnvioPendienteDto AEnvioDto(Envios e) => new(
            e.Id,
            e.OrigenDir,
            e.DestinoDir,
            e.Descripcion,
            e.Monto,
            (EstadoEnvio)e.Estado,
            EstadosEnvio.Texto(e.Estado),
            e.FechaLimite);

        private sealed record Ocurrencia(Viajes Viaje, DateOnly Fecha);
    }
}

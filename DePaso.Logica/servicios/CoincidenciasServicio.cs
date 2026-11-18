using DePaso.Entidades;
using DePaso.Logica.dtos;
using DePaso.Logica.enums;
using DePaso.Logica.helpers;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Logica.servicios;

/// <summary>
/// Decide qué envíos de otras personas le sirven a cada salida de un viaje activo.
/// La salida se identifica por viaje + fecha; las asignaciones ya confirmadas para esa fecha se restan.
/// </summary>
public sealed class CoincidenciasServicio : IServicioCoincidencias
{
    private readonly DePasoAppContext _context;
    private readonly IEstimadorDesvio _estimador;
    private readonly IOptimizadorCombinaciones _optimizador;
    private readonly IServicioRutas _rutas;
    private readonly TimeProvider _reloj;

    public CoincidenciasServicio(
        DePasoAppContext context,
        IEstimadorDesvio estimador,
        IOptimizadorCombinaciones optimizador,
        IServicioRutas rutas,
        TimeProvider? reloj = null
    )
    {
        _context = context;
        _estimador = estimador;
        _optimizador = optimizador;
        _rutas = rutas;
        _reloj = reloj ?? TimeProvider.System;
    }

    public async Task<Resultado<ListaCoincidenciasDto>> ListarAsync(
        int viajeId,
        int usuarioId,
        DateOnly fecha,
        CancellationToken ct = default
    )
    {
        var salidaRes = await CargarSalidaAsync(viajeId, usuarioId, fecha, ct);
        if (!salidaRes.Exito)
        {
            return salidaRes.Propagar<ListaCoincidenciasDto>();
        }
        var salida = salidaRes.Valor!;

        var ruta = ReglasCoincidencia.ConstruirRuta(salida.Viaje);
        var coincidencias = Evaluar(salida, ruta)
            .Where(x => x.Entra)
            .OrderByDescending(x => x.Envio.Monto)
            .ThenBy(x => x.DesvioKm)
            .ThenBy(x => x.Envio.Id)
            .Select(ACoincidencia)
            .ToList();

        return Resultado<ListaCoincidenciasDto>.Exitoso(
            new ListaCoincidenciasDto(
                viajeId,
                $"{salida.Viaje.OrigenCalle} → {salida.Viaje.DestinoCalle}",
                fecha,
                ACapacidad(salida.Restantes),
                coincidencias
            )
        );
    }

    public async Task<Resultado<DetalleCoincidenciaDto>> DetalleAsync(
        int viajeId,
        int envioId,
        int usuarioId,
        DateOnly fecha,
        CancellationToken ct = default
    )
    {
        var salidaRes = await CargarSalidaAsync(viajeId, usuarioId, fecha, ct);
        if (!salidaRes.Exito)
        {
            return salidaRes.Propagar<DetalleCoincidenciaDto>();
        }
        var salida = salidaRes.Valor!;

        // Un envío ajeno puede no estar disponible para esta salida: el detalle lo explica en vez de ocultarlo.
        var envio = await _context.Envios.AsNoTracking().FirstOrDefaultAsync(e => e.Id == envioId, ct);
        if (envio is null || envio.UsuarioId == usuarioId)
        {
            return Resultado<DetalleCoincidenciaDto>.Fallo(TipoError.NoEncontrado, "Envío no encontrado.");
        }

        var ruta = ReglasCoincidencia.ConstruirRuta(salida.Viaje);
        var desvio = ReglasCoincidencia.Desvio(_estimador, ruta, envio);
        var motivos = new List<string>(ReglasCoincidencia.Evaluar(envio, salida.Restantes, desvio).Motivos);

        var buscando = (byte)EstadoEnvio.BuscandoConductor;
        var asignado = await _context.Asignaciones.AnyAsync(a => a.EnvioId == envioId, ct);
        if (envio.Estado != buscando)
        {
            motivos.Insert(0, $"El envío está en estado '{EstadosEnvio.Texto(envio.Estado)}'.");
        }
        else if (asignado)
        {
            motivos.Insert(0, "El envío ya tiene conductor asignado.");
        }
        if (envio.FechaLimite < fecha)
        {
            motivos.Add("La fecha límite del envío es anterior a la salida.");
        }

        var rutaConEnvio = ReglasCoincidencia.RutaConEnvio(
            _estimador,
            ruta,
            ReglasCoincidencia.OrigenDe(envio),
            ReglasCoincidencia.DestinoDe(envio)
        );
        var habitual = await _rutas.CalcularRutaAsync(ruta, ct);
        var conEnvio = await _rutas.CalcularRutaAsync(rutaConEnvio, ct);

        var restantes = salida.Restantes;
        var capacidadQueda = new CapacidadDto(
            restantes.MaxPaquetes - 1,
            restantes.PesoMaxKg - envio.PesoKg,
            restantes.DesvioMaxKm - desvio
        );

        return Resultado<DetalleCoincidenciaDto>.Exitoso(
            new DetalleCoincidenciaDto(
                envio.Id,
                Origen(envio),
                Destino(envio),
                new RutaResumenDto(habitual.DistanciaKm, habitual.DuracionMin),
                new RutaResumenDto(conEnvio.DistanciaKm, conEnvio.DuracionMin),
                desvio,
                envio.PesoKg,
                capacidadQueda,
                envio.Monto,
                motivos.Count == 0,
                motivos
            )
        );
    }

    public async Task<Resultado<OptimizacionCoincidenciasDto>> OptimizarAsync(
        int viajeId,
        int usuarioId,
        DateOnly fecha,
        CancellationToken ct = default
    )
    {
        var salidaRes = await CargarSalidaAsync(viajeId, usuarioId, fecha, ct);
        if (!salidaRes.Exito)
        {
            return salidaRes.Propagar<OptimizacionCoincidenciasDto>();
        }
        var salida = salidaRes.Valor!;

        var evaluados = Evaluar(salida, ReglasCoincidencia.ConstruirRuta(salida.Viaje));
        var elegibles = evaluados
            .Where(x => x.Entra)
            .Select(x => new CandidatoEnvio(x.Envio.Id, x.Envio.Monto, x.Envio.PesoKg, x.DesvioKm))
            .ToList();

        var combinacion = _optimizador.Optimizar(elegibles, salida.Restantes);
        var recomendados = combinacion.Envios.Select(c => c.EnvioId).ToHashSet();

        // El optimizador sólo evalúa los MAX_CANDIDATOS con mayor pago; el resto se informa como fuera del tope.
        var conPago = elegibles
            .Where(c => c.Ganancia > 0)
            .OrderByDescending(c => c.Ganancia)
            .ThenBy(c => c.DesvioKm)
            .ThenBy(c => c.EnvioId)
            .ToList();
        var enTope = conPago
            .Take(OptimizadorCombinaciones.MAX_CANDIDATOS)
            .Select(c => c.EnvioId)
            .ToHashSet();

        var opciones = evaluados
            .Select(x => AOpcion(x, recomendados, enTope))
            .OrderBy(o => o.Estado)
            .ThenByDescending(o => o.Pago)
            .ThenBy(o => o.DesvioKm)
            .ThenBy(o => o.EnvioId)
            .ToList();

        var recomendada = new CombinacionDto(
            combinacion.Envios.Select(c => c.EnvioId).ToList(),
            combinacion.Cantidad,
            combinacion.PesoKg,
            combinacion.DesvioKm,
            combinacion.Ganancia
        );

        return Resultado<OptimizacionCoincidenciasDto>.Exitoso(
            new OptimizacionCoincidenciasDto(
                viajeId,
                fecha,
                recomendada,
                opciones,
                TopeAlcanzado: conPago.Count > OptimizadorCombinaciones.MAX_CANDIDATOS,
                TopeCandidatos: OptimizadorCombinaciones.MAX_CANDIDATOS
            )
        );
    }

    public async Task<Resultado<ResultadoAceptacionDto>> AceptarAsync(
        int viajeId,
        int usuarioId,
        DateOnly fecha,
        IReadOnlyList<int> envioIds,
        CancellationToken ct = default
    )
    {
        if (envioIds.Count == 0)
        {
            return Resultado<ResultadoAceptacionDto>.Invalido("Elegí al menos un envío.");
        }

        var salidaRes = await CargarSalidaAsync(viajeId, usuarioId, fecha, ct);
        if (!salidaRes.Exito)
        {
            return salidaRes.Propagar<ResultadoAceptacionDto>();
        }
        var salida = salidaRes.Valor!;

        var ruta = ReglasCoincidencia.ConstruirRuta(salida.Viaje);
        var habitual = await _rutas.CalcularRutaAsync(ruta, ct);
        var ahora = _reloj.GetLocalNow().DateTime;
        var buscando = (byte)EstadoEnvio.BuscandoConductor;

        var aceptados = new List<int>();
        var tomados = new List<FallaAceptacionDto>();
        var noEntran = new List<FallaAceptacionDto>();
        // Capacidad que queda, descontando lo que este mismo lote ya aceptó.
        var limites = salida.Restantes;

        foreach (var envioId in envioIds.Distinct())
        {
            // Se carga de nuevo en cada vuelta: si un SaveChanges falla, el tracker se limpia.
            var envio = await _context.Envios.FirstOrDefaultAsync(e => e.Id == envioId, ct);
            if (envio is null)
            {
                noEntran.Add(new FallaAceptacionDto(envioId, "El envío no existe."));
                continue;
            }
            if (envio.UsuarioId == usuarioId)
            {
                noEntran.Add(new FallaAceptacionDto(envioId, "Es un envío tuyo."));
                continue;
            }
            var asignacionPrevia = await _context
                .Asignaciones.AsNoTracking()
                .Include(a => a.Viaje)
                .FirstOrDefaultAsync(a => a.EnvioId == envioId, ct);
            if (asignacionPrevia is not null)
            {
                var mensaje =
                    asignacionPrevia.Viaje.UsuarioId == usuarioId
                        ? "Ya lo tenés asignado en Mis transportes."
                        : "Otro conductor ya lo tomó.";
                tomados.Add(new FallaAceptacionDto(envioId, mensaje));
                continue;
            }
            if (envio.Estado != buscando)
            {
                noEntran.Add(new FallaAceptacionDto(envioId, $"Ya no está en '{EstadosEnvio.Texto(envio.Estado)}'."));
                continue;
            }
            if (envio.FechaLimite < fecha)
            {
                noEntran.Add(new FallaAceptacionDto(envioId, "La fecha límite es anterior a la salida."));
                continue;
            }

            var desvio = ReglasCoincidencia.Desvio(_estimador, ruta, envio);
            var evaluado = ReglasCoincidencia.Evaluar(envio, limites, desvio);
            if (!evaluado.Entra)
            {
                noEntran.Add(new FallaAceptacionDto(envioId, string.Join(" ", evaluado.Motivos)));
                continue;
            }

            var conEnvio = await _rutas.CalcularRutaAsync(
                ReglasCoincidencia.RutaConEnvio(
                    _estimador,
                    ruta,
                    ReglasCoincidencia.OrigenDe(envio),
                    ReglasCoincidencia.DestinoDe(envio)
                ),
                ct
            );

            var anterior = envio.Estado;
            envio.Estado = (byte)EstadoEnvio.ConductorAsignado;
            _context.Asignaciones.Add(
                new Asignaciones
                {
                    EnvioId = envioId,
                    ViajeId = viajeId,
                    FechaViaje = fecha,
                    DesvioKm = desvio,
                    TiempoExtraMin = Math.Max(0, conEnvio.DuracionMin - habitual.DuracionMin),
                    Ganancia = envio.Monto,
                    AsignadoEn = ahora,
                }
            );
            _context.EnvioEstadoHistoria.Add(
                new EnvioEstadoHistoria
                {
                    EnvioId = envioId,
                    EstadoAnterior = anterior,
                    EstadoNuevo = (byte)EstadoEnvio.ConductorAsignado,
                    Fecha = ahora,
                }
            );

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Sólo es "tomado" si la asignación existe: si no, el error es otro y no se debe reportar como conflicto.
                _context.ChangeTracker.Clear();
                var tomado = await _context.Asignaciones.AsNoTracking().AnyAsync(a => a.EnvioId == envioId, ct);
                if (tomado)
                {
                    tomados.Add(new FallaAceptacionDto(envioId, "Otro conductor lo tomó recién."));
                }
                else
                {
                    noEntran.Add(new FallaAceptacionDto(envioId, "No se pudo guardar la reserva. Intentá de nuevo."));
                }
                continue;
            }

            aceptados.Add(envioId);
            limites = new LimitesViaje(
                limites.MaxPaquetes - 1,
                limites.PesoMaxKg - envio.PesoKg,
                limites.DesvioMaxKm - desvio
            );
        }

        return Resultado<ResultadoAceptacionDto>.Exitoso(
            new ResultadoAceptacionDto(viajeId, fecha, aceptados, tomados, noEntran)
        );
    }

    private async Task<Resultado<Salida>> CargarSalidaAsync(
        int viajeId,
        int usuarioId,
        DateOnly fecha,
        CancellationToken ct
    )
    {
        var viaje = await _context
            .Viajes.AsNoTracking()
            .Include(v => v.ViajeDia)
            .Include(v => v.Paradas)
            .FirstOrDefaultAsync(v => v.Id == viajeId, ct);
        if (viaje is null)
        {
            return Resultado<Salida>.Fallo(TipoError.NoEncontrado, "Viaje no encontrado.");
        }
        if (viaje.UsuarioId != usuarioId)
        {
            return Resultado<Salida>.Fallo(TipoError.Prohibido, "El viaje no pertenece al usuario.");
        }
        if (viaje.Activo != true)
        {
            return Resultado<Salida>.Fallo(TipoError.Conflicto, "El viaje no está activo.");
        }

        var ahora = _reloj.GetLocalNow().DateTime;
        var hoy = DateOnly.FromDateTime(ahora);
        if (fecha < hoy || (fecha == hoy && viaje.HoraSalida <= TimeOnly.FromDateTime(ahora)))
        {
            return Resultado<Salida>.Invalido("La salida ya pasó; elegí una fecha futura.");
        }
        if (!viaje.ViajeDia.Any(d => d.DiaSemana == DiasSemana.Numero(fecha.DayOfWeek)))
        {
            return Resultado<Salida>.Invalido("El viaje no sale ese día.");
        }

        var ocupacion = await _context
            .Asignaciones.AsNoTracking()
            .Where(a => a.ViajeId == viajeId && a.FechaViaje == fecha)
            .Select(a => new ReglasCoincidencia.OcupacionSalida(
                a.ViajeId,
                a.FechaViaje,
                a.Envio.PesoKg,
                a.DesvioKm
            ))
            .ToListAsync(ct);

        var buscando = (byte)EstadoEnvio.BuscandoConductor;
        var candidatos = await _context
            .Envios.AsNoTracking()
            .Where(e =>
                e.UsuarioId != usuarioId
                && e.Estado == buscando
                && e.FechaLimite >= fecha
                && !_context.Asignaciones.Any(a => a.EnvioId == e.Id)
            )
            .OrderBy(e => e.Id)
            .ToListAsync(ct);

        return Resultado<Salida>.Exitoso(
            new Salida(viaje, ReglasCoincidencia.Restantes(viaje, fecha, ocupacion), candidatos)
        );
    }

    private List<ReglasCoincidencia.Evaluado> Evaluar(Salida salida, List<Coordenada> ruta) =>
        ReglasCoincidencia.Evaluar(
            salida.Candidatos,
            salida.Restantes,
            e => ReglasCoincidencia.Desvio(_estimador, ruta, e)
        );

    private static OpcionCoincidenciaDto AOpcion(
        ReglasCoincidencia.Evaluado x,
        HashSet<int> recomendados,
        HashSet<int> enTope
    )
    {
        var id = x.Envio.Id;
        EstadoOpcionCoincidencia estado;
        IReadOnlyList<string> motivos;

        if (recomendados.Contains(id))
        {
            estado = EstadoOpcionCoincidencia.Recomendado;
            motivos = Array.Empty<string>();
        }
        else if (!x.Entra)
        {
            estado = EstadoOpcionCoincidencia.NoEntra;
            motivos = x.Motivos;
        }
        else if (x.Envio.Monto <= 0)
        {
            estado = EstadoOpcionCoincidencia.NoEntra;
            motivos = new[] { "Pago cero: no suma a la combinación." };
        }
        else if (!enTope.Contains(id))
        {
            estado = EstadoOpcionCoincidencia.NoEntra;
            motivos = new[]
            {
                $"Fuera de los {OptimizadorCombinaciones.MAX_CANDIDATOS} envíos con mayor pago que evalúa la combinación.",
            };
        }
        else
        {
            estado = EstadoOpcionCoincidencia.Alternativo;
            motivos = new[] { "Entra en la salida, pero no mejora la combinación recomendada." };
        }

        return new OpcionCoincidenciaDto(
            id,
            Origen(x.Envio),
            Destino(x.Envio),
            x.Envio.PesoKg,
            x.DesvioKm,
            x.Envio.Monto,
            estado,
            NivelesCoincidencia.Texto(estado),
            motivos
        );
    }

    private static CoincidenciaDto ACoincidencia(ReglasCoincidencia.Evaluado x)
    {
        var nivel = ReglasCoincidencia.Nivel(x.DesvioKm);
        return new CoincidenciaDto(
            x.Envio.Id,
            Origen(x.Envio),
            Destino(x.Envio),
            x.Envio.PesoKg,
            x.DesvioKm,
            x.Envio.Monto,
            nivel,
            NivelesCoincidencia.Texto(nivel)
        );
    }

    private static CapacidadDto ACapacidad(LimitesViaje limites) =>
        new(limites.MaxPaquetes, limites.PesoMaxKg, limites.DesvioMaxKm);

    private static string Origen(Envios e) =>
        $"{e.OrigenCalle} {e.OrigenAltura}, {e.OrigenLocalidad}, {e.OrigenProvincia}";

    private static string Destino(Envios e) =>
        $"{e.DestinoCalle} {e.DestinoAltura}, {e.DestinoLocalidad}, {e.DestinoProvincia}";

    private sealed record Salida(Viajes Viaje, LimitesViaje Restantes, List<Envios> Candidatos);
}

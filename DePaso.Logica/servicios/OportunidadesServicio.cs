using DePaso.Entidades;
using DePaso.Logica.dtos;
using DePaso.Logica.helpers;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Logica.servicios;

/// <summary>
/// Lista global de próximas salidas de los viajes activos del conductor, con la cantidad de envíos que
/// coinciden en cada una. Cada salida abre la lista de coincidencias de ese viaje y fecha.
/// </summary>
public sealed class OportunidadesServicio : IOportunidadesServicio
{
    public const int DIAS_VENTANA = 14;

    private readonly DePasoAppContext _context;
    private readonly IServicioCoincidencias _coincidencias;
    private readonly TimeProvider _reloj;

    public OportunidadesServicio(
        DePasoAppContext context,
        IServicioCoincidencias coincidencias,
        TimeProvider? reloj = null
    )
    {
        _context = context;
        _coincidencias = coincidencias;
        _reloj = reloj ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<OportunidadSalidaDto>> ListarSalidasAsync(
        int usuarioId,
        CancellationToken ct = default
    )
    {
        var ahora = _reloj.GetLocalNow().DateTime;
        var viajes = await _context
            .Viajes.AsNoTracking()
            .Include(v => v.ViajeDia)
            .Include(v => v.Paradas)
            .Where(v => v.UsuarioId == usuarioId && v.Activo == true)
            .ToListAsync(ct);

        var ocurrencias = ReglasCoincidencia.GenerarOcurrencias(
            viajes,
            DateOnly.FromDateTime(ahora),
            TimeOnly.FromDateTime(ahora),
            DIAS_VENTANA
        );

        var salidas = new List<OportunidadSalidaDto>(ocurrencias.Count);
        foreach (var o in ocurrencias)
        {
            var lista = await _coincidencias.ListarAsync(o.Viaje.Id, usuarioId, o.Fecha, ct);
            salidas.Add(
                new OportunidadSalidaDto(
                    o.Viaje.Id,
                    $"{o.Viaje.OrigenCalle} {o.Viaje.OrigenAltura}, {o.Viaje.OrigenLocalidad}",
                    $"{o.Viaje.DestinoCalle} {o.Viaje.DestinoAltura}, {o.Viaje.DestinoLocalidad}",
                    o.Fecha,
                    o.Viaje.HoraSalida,
                    lista.Exito ? lista.Valor!.Coincidencias.Count : 0
                )
            );
        }

        return salidas;
    }
}

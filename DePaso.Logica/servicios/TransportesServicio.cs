using DePaso.Entidades;
using DePaso.Logica.dtos;
using DePaso.Logica.enums;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Logica.servicios;

/// <summary>
/// Mis transportes: envíos que el conductor aceptó y el avance de su estado.
/// El orden de estados es fijo: Conductor asignado → Retirado → En camino → Entregado.
/// </summary>
public sealed class TransportesServicio : ITransportesServicio
{
    private static readonly EstadoEnvio[] Secuencia =
    {
        EstadoEnvio.ConductorAsignado,
        EstadoEnvio.Retirado,
        EstadoEnvio.EnCamino,
        EstadoEnvio.Entregado,
    };

    private readonly DePasoAppContext _context;
    private readonly TimeProvider _reloj;

    public TransportesServicio(DePasoAppContext context, TimeProvider? reloj = null)
    {
        _context = context;
        _reloj = reloj ?? TimeProvider.System;
    }

    public static EstadoEnvio? Siguiente(byte estado)
    {
        var indice = Array.IndexOf(Secuencia, (EstadoEnvio)estado);
        return indice >= 0 && indice < Secuencia.Length - 1 ? Secuencia[indice + 1] : null;
    }

    public async Task<IReadOnlyList<TransporteDto>> ListarAsync(int usuarioId, CancellationToken ct = default)
    {
        var asignaciones = await _context
            .Asignaciones.AsNoTracking()
            .Include(a => a.Envio)
            .Include(a => a.Viaje)
            .Where(a => a.Viaje.UsuarioId == usuarioId)
            .OrderBy(a => a.FechaViaje)
            .ThenBy(a => a.Viaje.HoraSalida)
            .ThenBy(a => a.Id)
            .ToListAsync(ct);

        return asignaciones.Select(ATransporte).ToList();
    }

    /// <summary>
    /// Pasa el envío al siguiente estado. <paramref name="estadoEsperado"/> es el estado que la pantalla mostraba:
    /// si ya cambió (otra pestaña, otro dispositivo), no se avanza y se pide recargar.
    /// </summary>
    public async Task<Resultado<TransporteDto>> AvanzarAsync(
        int asignacionId,
        int usuarioId,
        byte estadoEsperado,
        CancellationToken ct = default
    )
    {
        var asignacion = await _context
            .Asignaciones.Include(a => a.Envio)
            .Include(a => a.Viaje)
            .FirstOrDefaultAsync(a => a.Id == asignacionId, ct);
        if (asignacion is null)
        {
            return Resultado<TransporteDto>.Fallo(TipoError.NoEncontrado, "Transporte no encontrado.");
        }
        if (asignacion.Viaje.UsuarioId != usuarioId)
        {
            return Resultado<TransporteDto>.Fallo(TipoError.Prohibido, "El transporte no es tuyo.");
        }

        var envio = asignacion.Envio;
        if (envio.Estado != estadoEsperado)
        {
            return Resultado<TransporteDto>.Fallo(
                TipoError.Conflicto,
                $"El envío cambió a '{EstadosEnvio.Texto(envio.Estado)}'. Recargá la página."
            );
        }

        var siguiente = Siguiente(envio.Estado);
        if (siguiente is null)
        {
            return Resultado<TransporteDto>.Fallo(TipoError.Conflicto, "El envío ya no tiene un paso siguiente.");
        }

        var anterior = envio.Estado;
        envio.Estado = (byte)siguiente.Value;
        _context.EnvioEstadoHistoria.Add(
            new EnvioEstadoHistoria
            {
                EnvioId = envio.Id,
                EstadoAnterior = anterior,
                EstadoNuevo = envio.Estado,
                Fecha = _reloj.GetLocalNow().DateTime,
            }
        );
        await _context.SaveChangesAsync(ct);

        return Resultado<TransporteDto>.Exitoso(ATransporte(asignacion));
    }

    /// <summary>
    /// Cancela una asignación antes de que el envío sea retirado. El envío vuelve a "Buscando conductor"
    /// y reaparece en Oportunidades. <paramref name="estadoEsperado"/> funciona igual que en <see cref="AvanzarAsync"/>.
    /// </summary>
    public async Task<Resultado<bool>> CancelarAsync(
        int asignacionId,
        int usuarioId,
        byte estadoEsperado,
        CancellationToken ct = default
    )
    {
        var asignacion = await _context
            .Asignaciones.Include(a => a.Envio)
            .Include(a => a.Viaje)
            .FirstOrDefaultAsync(a => a.Id == asignacionId, ct);
        if (asignacion is null)
        {
            return Resultado<bool>.Fallo(TipoError.NoEncontrado, "Transporte no encontrado.");
        }
        if (asignacion.Viaje.UsuarioId != usuarioId)
        {
            return Resultado<bool>.Fallo(TipoError.Prohibido, "El transporte no es tuyo.");
        }

        var envio = asignacion.Envio;
        if (envio.Estado != estadoEsperado)
        {
            return Resultado<bool>.Fallo(
                TipoError.Conflicto,
                $"El envío cambió a '{EstadosEnvio.Texto(envio.Estado)}'. Recargá la página."
            );
        }
        if (envio.Estado != (byte)EstadoEnvio.ConductorAsignado)
        {
            return Resultado<bool>.Fallo(
                TipoError.Conflicto,
                "Solo podés cancelar antes de retirar el envío."
            );
        }

        var ahora = _reloj.GetLocalNow().DateTime;
        _context.Asignaciones.Remove(asignacion);
        envio.Estado = (byte)EstadoEnvio.BuscandoConductor;
        _context.EnvioEstadoHistoria.Add(
            new EnvioEstadoHistoria
            {
                EnvioId = envio.Id,
                EstadoAnterior = (byte)EstadoEnvio.ConductorAsignado,
                EstadoNuevo = (byte)EstadoEnvio.BuscandoConductor,
                Fecha = ahora,
            }
        );
        await _context.SaveChangesAsync(ct);

        return Resultado<bool>.Exitoso(true);
    }

    private static TransporteDto ATransporte(Asignaciones a) =>
        new(
            a.Id,
            a.EnvioId,
            $"{a.Envio.OrigenCalle} {a.Envio.OrigenAltura}, {a.Envio.OrigenLocalidad}, {a.Envio.OrigenProvincia}",
            $"{a.Envio.DestinoCalle} {a.Envio.DestinoAltura}, {a.Envio.DestinoLocalidad}, {a.Envio.DestinoProvincia}",
            a.Envio.PesoKg,
            a.Ganancia,
            a.FechaViaje,
            a.Viaje.HoraSalida,
            a.Envio.Estado,
            EstadosEnvio.Texto(a.Envio.Estado),
            (byte?)Siguiente(a.Envio.Estado),
            Siguiente(a.Envio.Estado) is { } s ? EstadosEnvio.Texto((byte)s) : null
        );
}

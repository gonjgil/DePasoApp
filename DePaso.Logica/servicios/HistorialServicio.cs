using System.Globalization;
using DePaso.Entidades;
using DePaso.Logica.Dtos;
using DePaso.Logica.enums;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Logica.servicios;

public class HistorialServicio : IHistorialServicio
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-AR");

    private readonly DePasoAppContext _context;

    public HistorialServicio(DePasoAppContext context)
    {
        _context = context;
    }

    public async Task<HistorialDto?> ObtenerAsync(int usuarioId, CancellationToken cancelacion = default)
    {
        var existe = await _context.Usuarios.AsNoTracking().AnyAsync(u => u.Id == usuarioId, cancelacion);
        if (!existe)
        {
            return null;
        }

        var entregado = (byte)EstadoEnvio.Entregado;
        var filas = await _context
            .Asignaciones.AsNoTracking()
            .Where(a => a.Viaje.UsuarioId == usuarioId && a.Envio.Estado == entregado)
            .Select(a => new Fila(
                a.ViajeId,
                a.FechaViaje,
                a.Ganancia,
                a.Viaje.OrigenLocalidad,
                a.Viaje.DestinoLocalidad,
                a.Envio.Descripcion
            ))
            .ToListAsync(cancelacion);

        var operaciones = filas
            .GroupBy(fila => (fila.ViajeId, fila.Fecha))
            .Select(grupo =>
            {
                var primera = grupo.First();
                var cantidad = grupo.Count();
                var detalle =
                    cantidad == 1
                        ? "Entregado · " + Texto(primera.Descripcion)
                        : $"{Mes(grupo.Key.Fecha)} · {cantidad} paquetes";
                return new OperacionHistorialDto(
                    primera.Origen,
                    primera.Destino,
                    detalle,
                    grupo.Sum(fila => fila.Ganancia),
                    grupo.Key.Fecha
                );
            })
            .OrderByDescending(operacion => operacion.Fecha)
            .ThenBy(operacion => operacion.Origen)
            .ToList();

        return new HistorialDto(
            operaciones.Count,
            filas.Count,
            filas.Sum(fila => fila.Ganancia),
            operaciones
        );
    }

    private static string Mes(DateOnly fecha)
    {
        var nombre = Es.DateTimeFormat.GetMonthName(fecha.Month);
        return Es.TextInfo.ToTitleCase(nombre);
    }

    private static string Texto(string? descripcion) =>
        string.IsNullOrWhiteSpace(descripcion) ? "Paquete" : descripcion.Trim();

    private sealed record Fila(
        int ViajeId,
        DateOnly Fecha,
        decimal Ganancia,
        string Origen,
        string Destino,
        string Descripcion
    );
}

using DePaso.Logica.Dtos;

namespace DePaso.Logica.servicios;

public interface IHistorialServicio
{
    Task<HistorialDto?> ObtenerAsync(int usuarioId, CancellationToken cancelacion = default);
}

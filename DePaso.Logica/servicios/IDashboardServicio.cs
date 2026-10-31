using DePaso.Logica.Dtos;

namespace DePaso.Logica.servicios
{
    public interface IDashboardServicio
    {
        Task<DashboardResumenDto?> ObtenerResumenAsync(int usuarioId, CancellationToken cancelacion = default);
    }
}
using DePaso.Logica.Dtos;

namespace DePaso.Logica.servicios
{
    public interface IDashboardServicio
    {
        Task<DashboardResumenDto?> ObtenerResumenAsync(
            int usuarioId,
            CancellationToken cancelacion = default);

        Task<PotencialMesDto?> ObtenerPotencialMesAsync(
            int usuarioId,
            string criterio,
            CancellationToken cancelacion = default);
    }
}
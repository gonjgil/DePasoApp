using DePaso.Logica.dtos;

namespace DePaso.Web.Models;

/// <summary>Resumen previo a reservar: los envíos elegidos de la lista y lo que ya no está disponible.</summary>
public sealed record ResumenCoincidenciasViewModel(
    int ViajeId,
    string Viaje,
    DateOnly Fecha,
    IReadOnlyList<CoincidenciaDto> Items,
    IReadOnlyList<int> NoDisponibles
)
{
    public decimal Pago => Items.Sum(i => i.Pago);
    public decimal KilosKg => Items.Sum(i => i.KilosKg);
    public decimal DesvioKm => Items.Sum(i => i.DesvioKm);
}

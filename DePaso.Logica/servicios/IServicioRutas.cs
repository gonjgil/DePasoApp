using DePaso.Logica.dtos;
using DePaso.Logica.helpers;

namespace DePaso.Logica.servicios;

public sealed record RutaCalculada(decimal DistanciaKm, int DuracionMin, bool Estimada);

public interface IServicioRutas
{
    Task<Coordenada?> GeocodificarAsync(DireccionDatos direccion, CancellationToken ct = default);
    Task<RutaCalculada> CalcularRutaAsync(IReadOnlyList<Coordenada> puntos, CancellationToken ct = default);
}

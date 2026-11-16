using DePaso.Logica.dtos;
using DePaso.Logica.helpers;

namespace DePaso.Logica.servicios;

public sealed class ServicioRutasMock : IServicioRutas
{
    public const double VelocidadPromedioKmh = 40;

    private const double LatCentro = -34.60;
    private const double LngCentro = -58.45;
    private const double AmplitudGrados = 0.30;

    private readonly IEstimadorDesvio _estimador;

    public ServicioRutasMock(IEstimadorDesvio? estimador = null)
    {
        _estimador = estimador ?? new EstimadorDesvioHaversine();
    }

    public Task<Coordenada?> GeocodificarAsync(DireccionDatos direccion, CancellationToken ct = default)
    {
        var hash = Fnv1a(TextoNormalizado.ClaveDireccion(direccion));
        var lat = LatCentro + ((hash & 0xFFFF) / 65535.0 - 0.5) * AmplitudGrados;
        var lng = LngCentro + (((hash >> 16) & 0xFFFF) / 65535.0 - 0.5) * AmplitudGrados;
        return Task.FromResult<Coordenada?>(new Coordenada(Math.Round(lat, 6), Math.Round(lng, 6)));
    }

    public Task<RutaCalculada> CalcularRutaAsync(IReadOnlyList<Coordenada> puntos, CancellationToken ct = default)
    {
        if (puntos.Count < 2)
        {
            throw new ArgumentException("La ruta necesita al menos origen y destino.", nameof(puntos));
        }

        double total = 0;
        for (var i = 1; i < puntos.Count; i++)
        {
            total += _estimador.DistanciaKm(puntos[i - 1], puntos[i]);
        }

        var minutos = (int)Math.Round(total / VelocidadPromedioKmh * 60d);
        if (minutos < 1 && total > 0)
        {
            minutos = 1;
        }

        return Task.FromResult(new RutaCalculada(Math.Round((decimal)total, 2), minutos, Estimada: true));
    }

    private static uint Fnv1a(string texto)
    {
        var hash = 2166136261u;
        foreach (var c in texto)
        {
            hash ^= c;
            hash *= 16777619u;
        }

        return hash;
    }
}
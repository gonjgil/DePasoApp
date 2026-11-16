using DePaso.Logica.helpers;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Text.Json;
using DePaso.Logica.servicios;
using DePaso.Logica.dtos;

namespace DePaso.Api.Services;

public sealed class ServicioRutasGeoapify : IServicioRutas
{
    private readonly HttpClient _http;
    private readonly string _clave;
    private readonly ServicioRutasMock _respaldo;
    private readonly ILogger<ServicioRutasGeoapify> _log;

    public ServicioRutasGeoapify(
        HttpClient http,
        IConfiguration configuracion,
        IEstimadorDesvio estimador,
        ILogger<ServicioRutasGeoapify> log)
    {
        _http = http;
        _clave = configuracion["Geoapify:ApiKey"]
            ?? throw new InvalidOperationException("Falta configurar Geoapify:ApiKey.");
        _respaldo = new ServicioRutasMock(estimador);
        _log = log;
    }

    public async Task<Coordenada?> GeocodificarAsync(DireccionDatos direccion, CancellationToken ct = default)
    {
        var texto = $"{direccion.Calle.Trim()} {direccion.Altura}, {direccion.Localidad.Trim()}, "
            + $"{direccion.Provincia.Trim()}, Argentina";

        var url = QueryHelpers.AddQueryString(
            "v1/geocode/search",
            new Dictionary<string, string?>
            {
                ["text"] = texto,
                ["filter"] = "countrycode:ar",
                ["lang"] = "es",
                ["limit"] = "5",
                ["format"] = "json",
                ["apiKey"] = _clave,
            });

        try
        {
            var respuesta = await _http.GetFromJsonAsync<Respuesta<ResultadoGeocoding>>(url, ct);
            var resultados = (respuesta?.Results ?? []).Where(r => r.Lat.HasValue && r.Lon.HasValue).ToList();

            var elegido = resultados.FirstOrDefault(r =>
                    int.TryParse(r.Housenumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                    && n == direccion.Altura)
                ?? resultados.FirstOrDefault();

            return elegido is null ? null : new Coordenada(elegido.Lat!.Value, elegido.Lon!.Value);
        }
        catch (Exception ex) when (EsErrorDeProveedor(ex, ct))
        {
            _log.LogWarning(ex, "Geoapify no pudo geocodificar '{Direccion}'.", texto);
            return null;
        }
    }

    public async Task<RutaCalculada> CalcularRutaAsync(IReadOnlyList<Coordenada> puntos, CancellationToken ct = default)
    {
        if (puntos.Count < 2)
        {
            throw new ArgumentException("La ruta necesita al menos origen y destino.", nameof(puntos));
        }

        var waypoints = string.Join("|", puntos.Select(p => FormattableString.Invariant($"{p.Lat},{p.Lng}")));
        var url = QueryHelpers.AddQueryString(
            "v1/routing",
            new Dictionary<string, string?>
            {
                ["waypoints"] = waypoints,
                ["mode"] = "drive",
                ["units"] = "metric",
                ["format"] = "json",
                ["apiKey"] = _clave,
            });

        try
        {
            var respuesta = await _http.GetFromJsonAsync<Respuesta<ResultadoRuta>>(url, ct);
            var ruta = respuesta?.Results?.FirstOrDefault();
            if (ruta is not null && ruta.Distance > 0)
            {
                var km = Math.Round(ruta.Distance / 1000m, 2);
                var minutos = Math.Max(1, (int)Math.Ceiling(ruta.Time / 60m));
                return new RutaCalculada(km, minutos, Estimada: false);
            }

            _log.LogWarning("Geoapify no devolvió ruta; se usa la estimación local.");
        }
        catch (Exception ex) when (EsErrorDeProveedor(ex, ct))
        {
            _log.LogWarning(ex, "Geoapify falló al calcular la ruta; se usa la estimación local.");
        }

        return await _respaldo.CalcularRutaAsync(puntos, ct);
    }

    private static bool EsErrorDeProveedor(Exception ex, CancellationToken ct) =>
        ex is HttpRequestException or JsonException or NotSupportedException
        || (ex is TaskCanceledException && !ct.IsCancellationRequested);

    private sealed record Respuesta<T>(T[]? Results);

    private sealed record ResultadoGeocoding(double? Lat, double? Lon, string? Housenumber);

    private sealed record ResultadoRuta(decimal Distance, decimal Time);
}
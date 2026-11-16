using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;

namespace DePaso.Api.Services;

public sealed class GeoapifyClient(HttpClient http, IConfiguration configuracion)
{
    public Task<DireccionGeoapify[]> BuscarAsync(string direccion, CancellationToken ct) =>
        ConsultarAsync<DireccionGeoapify>(
            "v1/geocode/search",
            new()
            {
                ["text"] = direccion,
                ["filter"] = "countrycode:ar",
                ["lang"] = "es",
                ["limit"] = "5",
            },
            ct
        );

    public async Task<RutaGeoapify?> CalcularRutaAsync(
        decimal origenLat,
        decimal origenLng,
        decimal destinoLat,
        decimal destinoLng,
        CancellationToken ct
    )
    {
        var puntos = FormattableString.Invariant(
            $"{origenLat},{origenLng}|{destinoLat},{destinoLng}"
        );

        var rutas = await ConsultarAsync<RutaGeoapify>(
            "v1/routing",
            new()
            {
                ["waypoints"] = puntos,
                ["mode"] = "drive",
                ["units"] = "metric",
            },
            ct
        );

        return rutas.FirstOrDefault();
    }

    private async Task<T[]> ConsultarAsync<T>(
        string recurso,
        Dictionary<string, string?> parametros,
        CancellationToken ct
    )
    {
        var clave = configuracion["Geoapify:ApiKey"];
        if (string.IsNullOrWhiteSpace(clave))
            throw new InvalidOperationException("Falta configurar Geoapify:ApiKey.");

        parametros["apiKey"] = clave;
        parametros["format"] = "json";

        var url = QueryHelpers.AddQueryString(recurso, parametros);
        var respuesta = await http.GetFromJsonAsync<RespuestaGeoapify<T>>(url, ct);
        return respuesta?.Results ?? [];
    }

    private sealed record RespuestaGeoapify<T>(T[]? Results);

    public Task<DireccionGeoapify[]> SugerirCallesAsync(string texto, CancellationToken ct) =>
        ConsultarAsync<DireccionGeoapify>(
            "v1/geocode/autocomplete",
            new()
            {
                ["text"] = texto,
                ["type"] = "street",
                ["filter"] = "countrycode:ar",
                ["lang"] = "es",
                ["limit"] = "5",
            },
            ct
        );
}

public sealed record DireccionGeoapify(
    string? Formatted,
    string? Street,
    string? Housenumber,
    string? City,
    string? Suburb,
    string? District,
    string? State,
    decimal? Lat,
    decimal? Lon,
    [property: JsonPropertyName("result_type")] string? Tipo
);

public sealed record RutaGeoapify(decimal Distance, decimal Time);

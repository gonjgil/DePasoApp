using System.Net;
using System.Text.Json;
using DePaso.Logica.dtos;

namespace DePaso.Web.Services;

public class OportunidadesApiClient
{
    private readonly HttpClient _httpClient;

    public OportunidadesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<OportunidadSalidaDto>> ObtenerSalidasAsync(int usuarioId)
    {
        var respuesta = await _httpClient.GetAsync($"api/oportunidades?usuarioId={usuarioId}");
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<List<OportunidadSalidaDto>>()
            ?? new List<OportunidadSalidaDto>();
    }

    public async Task<ListaCoincidenciasDto?> ListarAsync(int viajeId, int usuarioId, DateOnly fecha)
    {
        var respuesta = await _httpClient.GetAsync(
            $"api/viajes/{viajeId}/coincidencias?usuarioId={usuarioId}&fecha={fecha:yyyy-MM-dd}"
        );
        if (respuesta.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ListaCoincidenciasDto>();
    }

    public async Task<DetalleCoincidenciaDto?> DetalleAsync(int viajeId, int envioId, int usuarioId, DateOnly fecha)
    {
        var respuesta = await _httpClient.GetAsync(
            $"api/viajes/{viajeId}/coincidencias/{envioId}?usuarioId={usuarioId}&fecha={fecha:yyyy-MM-dd}"
        );
        if (respuesta.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<DetalleCoincidenciaDto>();
    }

    public async Task<OptimizacionCoincidenciasDto?> OptimizarAsync(int viajeId, int usuarioId, DateOnly fecha)
    {
        var respuesta = await _httpClient.GetAsync(
            $"api/viajes/{viajeId}/coincidencias/optimizar?usuarioId={usuarioId}&fecha={fecha:yyyy-MM-dd}"
        );
        if (respuesta.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<OptimizacionCoincidenciasDto>();
    }

    public async Task<(ResultadoAceptacionDto? Resultado, string? Error)> AceptarAsync(
        int viajeId,
        int usuarioId,
        DateOnly fecha,
        IReadOnlyList<int> envioIds
    )
    {
        var respuesta = await _httpClient.PostAsJsonAsync(
            $"api/viajes/{viajeId}/coincidencias/aceptar",
            new { UsuarioId = usuarioId, Fecha = fecha, EnvioIds = envioIds }
        );

        if (!respuesta.IsSuccessStatusCode)
        {
            return (null, await LeerErrorAsync(respuesta, "No se pudieron reservar los envíos."));
        }

        return (await respuesta.Content.ReadFromJsonAsync<ResultadoAceptacionDto>(), null);
    }

    private static async Task<string> LeerErrorAsync(HttpResponseMessage respuesta, string porDefecto)
    {
        try
        {
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            if (raiz.TryGetProperty("errores", out var errores) && errores.ValueKind == JsonValueKind.Array)
            {
                var unidos = string.Join(" ", errores.EnumerateArray().Select(e => e.GetString()).Where(t => !string.IsNullOrWhiteSpace(t)));
                if (unidos.Length > 0)
                {
                    return unidos;
                }
            }

            if (raiz.TryGetProperty("mensaje", out var mensaje) && mensaje.ValueKind == JsonValueKind.String)
            {
                return mensaje.GetString()!;
            }
        }
        catch (JsonException) { }

        return porDefecto;
    }
}

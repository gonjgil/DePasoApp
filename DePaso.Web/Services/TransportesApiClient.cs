using System.Text.Json;
using DePaso.Logica.dtos;

namespace DePaso.Web.Services;

public class TransportesApiClient
{
    private readonly HttpClient _httpClient;

    public TransportesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TransporteDto>> ListarAsync(int usuarioId)
    {
        var respuesta = await _httpClient.GetAsync($"api/transportes?usuarioId={usuarioId}");
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<List<TransporteDto>>() ?? new List<TransporteDto>();
    }

    public Task<string?> AvanzarAsync(int asignacionId, int usuarioId, byte estadoEsperado) =>
        EnviarAccionAsync("avanzar", asignacionId, usuarioId, estadoEsperado);

    public Task<string?> CancelarAsync(int asignacionId, int usuarioId, byte estadoEsperado) =>
        EnviarAccionAsync("cancelar", asignacionId, usuarioId, estadoEsperado);

    private async Task<string?> EnviarAccionAsync(string accion, int asignacionId, int usuarioId, byte estadoEsperado)
    {
        var respuesta = await _httpClient.PatchAsync(
            $"api/transportes/{asignacionId}/{accion}?usuarioId={usuarioId}&estadoEsperado={estadoEsperado}",
            content: null
        );
        if (respuesta.IsSuccessStatusCode)
        {
            return null;
        }

        try
        {
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            if (json.RootElement.TryGetProperty("mensaje", out var mensaje) && mensaje.ValueKind == JsonValueKind.String)
            {
                return mensaje.GetString();
            }
        }
        catch (JsonException) { }

        return "No se pudo avanzar el envío.";
    }
}

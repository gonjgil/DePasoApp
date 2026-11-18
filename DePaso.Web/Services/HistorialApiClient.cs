using System.Net;
using DePaso.Logica.Dtos;

namespace DePaso.Web.Services;

public class HistorialApiClient
{
    private readonly HttpClient _httpClient;

    public HistorialApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HistorialDto?> ObtenerAsync(int usuarioId, CancellationToken cancelacion)
    {
        var respuesta = await _httpClient.GetAsync($"api/historial?usuarioId={usuarioId}", cancelacion);
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<HistorialDto>(cancelacion);
    }
}

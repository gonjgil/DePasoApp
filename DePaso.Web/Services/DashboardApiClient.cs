using System.Net;
using DePaso.Logica.Dtos;
using DePaso.Logica.servicios;

namespace DePaso.Web.Services;

public sealed class DashboardApiClient : IDashboardServicio
{
    private readonly HttpClient _httpClient;

    public DashboardApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<DashboardResumenDto?> ObtenerResumenAsync(
        int usuarioId,
        CancellationToken cancelacion = default)
    {
        using var respuesta = await _httpClient.GetAsync(
            $"api/dashboard?usuarioId={usuarioId}",
            cancelacion
        );

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<DashboardResumenDto>(cancelacion);
    }

    public async Task<PotencialMesDto?> ObtenerPotencialMesAsync(
        int usuarioId,
        string criterio,
        CancellationToken cancelacion = default)
    {
        var criterioEscapado = Uri.EscapeDataString(criterio);
        using var respuesta = await _httpClient.GetAsync(
            $"api/dashboard/potencial?usuarioId={usuarioId}&criterio={criterioEscapado}",
            cancelacion
        );

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<PotencialMesDto>(cancelacion);
    }
}

using System.Net;
using System.Net.Http.Json;
using DePaso.Web.Models;

namespace DePaso.Web.Services;

public class EnviosApiClient
{
    private readonly HttpClient _httpClient;

    public EnviosApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<EnvioApiResponse>> ObtenerMiosAsync(int usuarioId)
    {
        var respuesta = await _httpClient.GetAsync($"api/envios/mios?usuarioId={usuarioId}");
        respuesta.EnsureSuccessStatusCode();

        return await respuesta.Content.ReadFromJsonAsync<List<EnvioApiResponse>>()
            ?? new List<EnvioApiResponse>();
    }

    public async Task<EnvioApiResponse?> ObtenerPorIdAsync(int id, int usuarioId)
    {
        var respuesta = await _httpClient.GetAsync($"api/envios/{id}?usuarioId={usuarioId}");
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<EnvioApiResponse>();
    }

    public async Task<(EnvioApiResponse? Envio, string? Error)> CrearAsync(
        int usuarioId,
        string origenCalle,
        int origenAltura,
        string? origenPiso,
        string? origenDepartamento,
        string origenLocalidad,
        string origenProvincia,
        string destinoCalle,
        int destinoAltura,
        string? destinoPiso,
        string? destinoDepartamento,
        string destinoLocalidad,
        string destinoProvincia,
        DateOnly fechaLimite,
        decimal pesoKg,
        byte tamanio,
        string descripcion,
        decimal monto
    )
    {
        var respuesta = await _httpClient.PostAsJsonAsync(
            "api/envios/crear",
            new
            {
                UsuarioId = usuarioId,
                Origen = new
                {
                    Calle = origenCalle,
                    Altura = origenAltura,
                    Piso = origenPiso,
                    Departamento = origenDepartamento,
                    Localidad = origenLocalidad,
                    Provincia = origenProvincia,
                },
                Destino = new
                {
                    Calle = destinoCalle,
                    Altura = destinoAltura,
                    Piso = destinoPiso,
                    Departamento = destinoDepartamento,
                    Localidad = destinoLocalidad,
                    Provincia = destinoProvincia,
                },
                FechaLimite = fechaLimite,
                PesoKg = pesoKg,
                Tamanio = tamanio,
                Descripcion = descripcion,
                Monto = monto,
            }
        );

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return (null, "Usuario no encontrado.");
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            var error = await respuesta.Content.ReadFromJsonAsync<ErrorApiResponse>();
            return (null, error?.Mensaje ?? "No se pudo crear el envío.");
        }

        var envio = await respuesta.Content.ReadFromJsonAsync<EnvioApiResponse>();
        return (envio, null);
    }

    public async Task<(EnvioApiResponse? Envio, string? Error)> EditarAsync(
        int id,
        int usuarioId,
        string origenCalle,
        int origenAltura,
        string? origenPiso,
        string? origenDepartamento,
        string origenLocalidad,
        string origenProvincia,
        string destinoCalle,
        int destinoAltura,
        string? destinoPiso,
        string? destinoDepartamento,
        string destinoLocalidad,
        string destinoProvincia,
        DateOnly fechaLimite,
        decimal pesoKg,
        byte tamanio,
        string descripcion,
        decimal monto
    )
    {
        var respuesta = await _httpClient.PutAsJsonAsync(
            $"api/envios/{id}",
            new
            {
                UsuarioId = usuarioId,
                Origen = new
                {
                    Calle = origenCalle,
                    Altura = origenAltura,
                    Piso = origenPiso,
                    Departamento = origenDepartamento,
                    Localidad = origenLocalidad,
                    Provincia = origenProvincia,
                },
                Destino = new
                {
                    Calle = destinoCalle,
                    Altura = destinoAltura,
                    Piso = destinoPiso,
                    Departamento = destinoDepartamento,
                    Localidad = destinoLocalidad,
                    Provincia = destinoProvincia,
                },
                FechaLimite = fechaLimite,
                PesoKg = pesoKg,
                Tamanio = tamanio,
                Descripcion = descripcion,
                Monto = monto,
            }
        );

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return (null, "Envío no encontrado.");
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            var error = await respuesta.Content.ReadFromJsonAsync<ErrorApiResponse>();
            return (null, error?.Mensaje ?? "No se pudo editar el envío.");
        }

        var envio = await respuesta.Content.ReadFromJsonAsync<EnvioApiResponse>();
        return (envio, null);
    }

    public async Task<bool> BorrarAsync(int id, int usuarioId)
    {
        var respuesta = await _httpClient.DeleteAsync($"api/envios/{id}?usuarioId={usuarioId}");
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        respuesta.EnsureSuccessStatusCode();
        return true;
    }
}

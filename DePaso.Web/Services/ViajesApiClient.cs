using System.Net;
using System.Text.Json;
using DePaso.Logica.dtos;
using DePaso.Web.Models;

namespace DePaso.Web.Services;

public class ViajesApiClient
{
    private readonly HttpClient _httpClient;

    public ViajesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ViajeResumenDto>> ObtenerMiosAsync(int usuarioId, string? estado)
    {
        var url = $"api/viajes?usuarioId={usuarioId}";
        if (!string.IsNullOrWhiteSpace(estado))
        {
            url += $"&estado={Uri.EscapeDataString(estado)}";
        }

        var respuesta = await _httpClient.GetAsync(url);
        respuesta.EnsureSuccessStatusCode();

        return await respuesta.Content.ReadFromJsonAsync<List<ViajeResumenDto>>()
            ?? new List<ViajeResumenDto>();
    }

    public async Task<ViajeDetalleDto?> ObtenerPorIdAsync(int id, int usuarioId)
    {
        var respuesta = await _httpClient.GetAsync($"api/viajes/{id}?usuarioId={usuarioId}");
        if (respuesta.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ViajeDetalleDto>();
    }

    public async Task<(ViajeDetalleDto? Viaje, string? Error)> CrearAsync(int usuarioId, NuevoViajeViewModel modelo)
    {
        var respuesta = await _httpClient.PostAsJsonAsync(
            "api/viajes",
            new
            {
                UsuarioId = usuarioId,
                Origen = Origen(modelo),
                Destino = Destino(modelo),
                Dias = modelo.Dias,
                HoraSalida = modelo.HoraSalida,
                HoraLlegada = modelo.HoraLlegada,
                MaxPaquetes = modelo.MaxPaquetes,
                PesoMaxKg = modelo.PesoMaxKg,
                DesvioMaxKm = modelo.DesvioMaxKm,
                Paradas = Paradas(modelo),
            }
        );

        if (!respuesta.IsSuccessStatusCode)
        {
            return (null, await LeerErrorAsync(respuesta, "No se pudo guardar el viaje."));
        }

        return (await respuesta.Content.ReadFromJsonAsync<ViajeDetalleDto>(), null);
    }

    public async Task<(RutaDto? Ruta, string? Error)> CalcularRutaAsync(NuevoViajeViewModel modelo)
    {
        var respuesta = await _httpClient.PostAsJsonAsync(
            "api/viajes/ruta",
            new
            {
                Origen = Origen(modelo),
                Destino = Destino(modelo),
                Paradas = Paradas(modelo),
            }
        );

        if (!respuesta.IsSuccessStatusCode)
        {
            return (null, await LeerErrorAsync(respuesta, "No se pudo calcular la ruta."));
        }

        return (await respuesta.Content.ReadFromJsonAsync<RutaDto>(), null);
    }

    public Task<string?> DesactivarAsync(int id, int usuarioId) =>
        CambiarEstadoAsync($"api/viajes/{id}/desactivar?usuarioId={usuarioId}", "No se pudo desactivar el viaje.");

    public Task<string?> ActivarAsync(int id, int usuarioId) =>
        CambiarEstadoAsync($"api/viajes/{id}/activar?usuarioId={usuarioId}", "No se pudo activar el viaje.");

    private async Task<string?> CambiarEstadoAsync(string url, string errorPorDefecto)
    {
        var respuesta = await _httpClient.PatchAsync(url, content: null);
        return respuesta.IsSuccessStatusCode ? null : await LeerErrorAsync(respuesta, errorPorDefecto);
    }

    private static object Origen(NuevoViajeViewModel m) => new
    {
        Calle = m.OrigenCalle,
        Altura = m.OrigenAltura.GetValueOrDefault(),
        Piso = m.OrigenPiso,
        Departamento = m.OrigenDepartamento,
        Localidad = m.OrigenLocalidad,
        Provincia = m.OrigenProvincia,
        Lat = m.OrigenLat,
        Lng = m.OrigenLng,
    };

    private static object Destino(NuevoViajeViewModel m) => new
    {
        Calle = m.DestinoCalle,
        Altura = m.DestinoAltura.GetValueOrDefault(),
        Piso = m.DestinoPiso,
        Departamento = m.DestinoDepartamento,
        Localidad = m.DestinoLocalidad,
        Provincia = m.DestinoProvincia,
        Lat = m.DestinoLat,
        Lng = m.DestinoLng,
    };

    private static List<object> Paradas(NuevoViajeViewModel m) =>
        (m.Paradas ?? new())
            .Select((p, i) => (object)new
            {
                Calle = p.Calle ?? "",
                Altura = p.Altura,
                Piso = p.Piso,
                Departamento = p.Departamento,
                Localidad = p.Localidad ?? "",
                Provincia = p.Provincia ?? "",
                Lat = p.Lat == 0 && p.Lng == 0 ? (decimal?)null : p.Lat,
                Lng = p.Lat == 0 && p.Lng == 0 ? (decimal?)null : p.Lng,
                Orden = i + 1,
            })
            .ToList();

    private static async Task<string> LeerErrorAsync(HttpResponseMessage respuesta, string porDefecto)
    {
        try
        {
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            if (raiz.TryGetProperty("errores", out var errores) && errores.ValueKind == JsonValueKind.Array)
            {
                var textos = errores.EnumerateArray().Select(e => e.GetString()).Where(t => !string.IsNullOrWhiteSpace(t));
                var unidos = string.Join(" ", textos);
                if (unidos.Length > 0)
                {
                    return unidos;
                }
            }

            if (raiz.TryGetProperty("mensaje", out var mensaje) && mensaje.ValueKind == JsonValueKind.String)
            {
                return mensaje.GetString()!;
            }

            if (raiz.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var textos = errors.EnumerateObject()
                    .SelectMany(campo => campo.Value.EnumerateArray())
                    .Select(e => e.GetString())
                    .Where(t => !string.IsNullOrWhiteSpace(t));
                var unidos = string.Join(" ", textos);
                if (unidos.Length > 0)
                {
                    return unidos;
                }
            }
        }
        catch (JsonException) {}

        return porDefecto;
    }
}

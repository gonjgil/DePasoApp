using DePaso.Logica.dtos;
using DePaso.Web.Models;
using DePaso.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Globalization;
using System.Security.Claims;

namespace DePaso.Web.Controllers;

public sealed class CulturaInvarianteAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}

[Authorize]
[CulturaInvariante]
public class ViajesController : Controller
{
    private const string MensajeSinApi = "No se pudo conectar con la API. Verifica que DePaso.Api este corriendo.";

    private static readonly string[] CamposQueNoAfectanLaRuta =
    [
        "Dias",
        "HoraSalida",
        "HoraLlegada",
        "MaxPaquetes",
        "PesoMaxKg",
        "DesvioMaxKm",
    ];

    private static readonly string[] FormatosHora = ["HH:mm", "HH:mm:ss", "H:mm", "HH:mm:ss.FFFFFFF"];

    private readonly ViajesApiClient _viajesApi;

    public ViajesController(ViajesApiClient viajesApi)
    {
        _viajesApi = viajesApi;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? estado)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        estado = estado?.Trim().ToLowerInvariant() switch
        {
            "activos" => "activos",
            "inactivos" => "inactivos",
            _ => "todos",
        };
        ViewData["Estado"] = estado;

        try
        {
            return View(await _viajesApi.ObtenerMiosAsync(usuarioId, estado));
        }
        catch (HttpRequestException)
        {
            ViewData["Error"] = MensajeSinApi;
            return View(new List<ViajeResumenDto>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        ViajeDetalleDto? viaje;
        try
        {
            viaje = await _viajesApi.ObtenerPorIdAsync(id, usuarioId);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = MensajeSinApi;
            return RedirectToAction(nameof(Index));
        }

        if (viaje is null)
        {
            TempData["Error"] = "No encontramos ese viaje.";
            return RedirectToAction(nameof(Index));
        }

        return View(viaje);
    }

    [HttpGet]
    public IActionResult Nuevo()
    {
        return View(new NuevoViajeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nuevo(NuevoViajeViewModel modelo, string? accion)
    {
        modelo.Paradas = (modelo.Paradas ?? new()).Where(parada => !parada.EstaVacia()).ToList();
        modelo.Dias = (modelo.Dias ?? new()).Where(dia => dia is >= 1 and <= 7).Distinct().ToList();
        modelo.HoraSalida = LeerHora(Request.Form["HoraSalida"]);
        modelo.HoraLlegada = LeerHora(Request.Form["HoraLlegada"]);

        ModelState.Clear();
        TryValidateModel(modelo);

        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        if (accion == "calcular")
        {
            foreach (var campo in CamposQueNoAfectanLaRuta)
            {
                ModelState.Remove(campo);
            }

            if (ModelState.IsValid)
            {
                await CalcularRutaAsync(modelo);
            }

            return View(modelo);
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        try
        {
            var (viaje, error) = await _viajesApi.CrearAsync(usuarioId, modelo);
            if (error is not null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(modelo);
            }

            TempData["Mensaje"] = "Guardamos tu viaje.";
            return RedirectToAction(nameof(Detalle), new { id = viaje!.Id });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, MensajeSinApi);
            return View(modelo);
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Desactivar(int id, string? volverA) =>
        CambiarEstadoAsync(id, volverA, desactivar: true);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Activar(int id, string? volverA) =>
        CambiarEstadoAsync(id, volverA, desactivar: false);

    private async Task<IActionResult> CambiarEstadoAsync(int id, string? volverA, bool desactivar)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        try
        {
            var error = desactivar
                ? await _viajesApi.DesactivarAsync(id, usuarioId)
                : await _viajesApi.ActivarAsync(id, usuarioId);

            if (error is null)
            {
                TempData["Mensaje"] = desactivar ? "El viaje quedó inactivo." : "El viaje volvió a estar activo.";
            }
            else
            {
                TempData["Error"] = error;
            }
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = MensajeSinApi;
        }

        return volverA == "lista"
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Detalle), new { id });
    }

    private async Task CalcularRutaAsync(NuevoViajeViewModel modelo)
    {
        try
        {
            var (ruta, error) = await _viajesApi.CalcularRutaAsync(modelo);
            if (error is not null)
            {
                ModelState.AddModelError(string.Empty, error);
                return;
            }

            AplicarRuta(modelo, ruta!);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, MensajeSinApi);
        }
    }

    private void AplicarRuta(NuevoViajeViewModel modelo, RutaDto ruta)
    {
        modelo.DistanciaKm = ruta.DistanciaKm;
        modelo.DuracionMin = ruta.DuracionMin;
        ViewData["RutaEstimada"] = ruta.Estimada;
        ViewData["RutaAdvertencia"] = ruta.Advertencia;

        foreach (var punto in ruta.Puntos)
        {
            switch (punto.Tipo)
            {
                case "Origen":
                    modelo.OrigenLat = punto.Lat;
                    modelo.OrigenLng = punto.Lng;
                    break;
                case "Destino":
                    modelo.DestinoLat = punto.Lat;
                    modelo.DestinoLng = punto.Lng;
                    break;
                case "Parada" when punto.Orden >= 1 && punto.Orden <= modelo.Paradas.Count:
                    modelo.Paradas[punto.Orden - 1].Lat = punto.Lat;
                    modelo.Paradas[punto.Orden - 1].Lng = punto.Lng;
                    break;
            }
        }

        foreach (var campo in new[] { "OrigenLat", "OrigenLng", "DestinoLat", "DestinoLng" })
        {
            ModelState.Remove(campo);
        }
    }

    private bool TryObtenerUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);

    private static TimeOnly? LeerHora(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        return TimeOnly.TryParseExact(
            texto.Trim(),
            FormatosHora,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var hora
        )
            ? hora
            : null;
    }
}
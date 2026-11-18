using DePaso.Logica.Dtos;
using DePaso.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DePaso.Web.Controllers;

[Authorize]
public class HistorialController : Controller
{
    private const string MensajeSinApi = "No se pudo conectar con la API. Verifica que DePaso.Api este corriendo.";

    private readonly HistorialApiClient _historial;

    public HistorialController(HistorialApiClient historial)
    {
        _historial = historial;
    }

    [HttpGet("/Historial")]
    public async Task<IActionResult> Historial(CancellationToken cancelacion)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claim, out var usuarioId))
        {
            return await CerrarSesionYVolverAIngresar();
        }

        try
        {
            var historial = await _historial.ObtenerAsync(usuarioId, cancelacion);
            if (historial is null)
            {
                return await CerrarSesionYVolverAIngresar();
            }

            return View(historial);
        }
        catch (HttpRequestException)
        {
            ViewData["Error"] = MensajeSinApi;
            return View(new HistorialDto(0, 0, 0m, []));
        }
    }

    private async Task<IActionResult> CerrarSesionYVolverAIngresar()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Ingresar", "Cuenta");
    }
}

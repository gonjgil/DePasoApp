using DePaso.Logica.servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DePaso.Web.Controllers;

[Authorize]
public class HistorialController : Controller
{
    private readonly IHistorialServicio _historial;

    public HistorialController(IHistorialServicio historial)
    {
        _historial = historial;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claim, out var usuarioId))
        {
            return await CerrarSesionYVolverAIngresar();
        }

        var historial = await _historial.ObtenerAsync(usuarioId, cancelacion);
        if (historial is null)
        {
            return await CerrarSesionYVolverAIngresar();
        }

        return View(historial);
    }

    private async Task<IActionResult> CerrarSesionYVolverAIngresar()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Ingresar", "Cuenta");
    }
}

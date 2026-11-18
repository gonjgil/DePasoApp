using DePaso.Logica.servicios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DePaso.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardServicio _dashboard;

        public DashboardController(IDashboardServicio dashboard)
        {
            _dashboard = dashboard;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancelacion)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var usuarioId))
            {
                return await CerrarSesionYVolverAIngresar();
            }

            var resumen = await _dashboard.ObtenerResumenAsync(usuarioId, cancelacion);
            if (resumen == null)
            {
                return await CerrarSesionYVolverAIngresar();
            }

            return View("Dashboard", resumen);
        }

        [HttpGet]
        public async Task<IActionResult> Potencial(string criterio = "ganancia",CancellationToken cancelacion = default)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claim, out var usuarioId))
            {
                return await CerrarSesionYVolverAIngresar();
            }

            var potencial = await _dashboard.ObtenerPotencialMesAsync(
                usuarioId,
                criterio,
                cancelacion
            );

            if (potencial == null)
            {
                return await CerrarSesionYVolverAIngresar();
            }

            return View("Potencial", potencial);
        }
        private async Task<IActionResult> CerrarSesionYVolverAIngresar()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Ingresar", "Cuenta");
        }
    }

}

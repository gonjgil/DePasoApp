using System.Security.Claims;
using DePaso.Logica.dtos;
using DePaso.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DePaso.Web.Controllers;

[Authorize]
[CulturaInvariante]
public class TransportesController : Controller
{
    private const string MensajeSinApi = "No se pudo conectar con la API. Verifica que DePaso.Api este corriendo.";

    private readonly TransportesApiClient _transportes;

    public TransportesController(TransportesApiClient transportes)
    {
        _transportes = transportes;
    }

    [HttpGet("/Transportes")]
    public async Task<IActionResult> MisTransportes()
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        try
        {
            return View(await _transportes.ListarAsync(usuarioId));
        }
        catch (HttpRequestException)
        {
            ViewData["Error"] = MensajeSinApi;
            return View(new List<TransporteDto>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Avanzar(int asignacionId, byte estadoEsperado)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        try
        {
            var error = await _transportes.AvanzarAsync(asignacionId, usuarioId, estadoEsperado);
            TempData[error is null ? "Mensaje" : "Error"] = error ?? "Estado actualizado.";
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = MensajeSinApi;
        }

        return RedirectToAction(nameof(MisTransportes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int asignacionId, byte estadoEsperado)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        try
        {
            var error = await _transportes.CancelarAsync(asignacionId, usuarioId, estadoEsperado);
            TempData[error is null ? "Mensaje" : "Error"] = error ?? "Asignación cancelada. El envío volvió a Oportunidades.";
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = MensajeSinApi;
        }

        return RedirectToAction(nameof(MisTransportes));
    }

    private bool TryObtenerUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}

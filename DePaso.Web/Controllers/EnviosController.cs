using System.Security.Claims;
using DePaso.Web.Models;
using DePaso.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DePaso.Web.Controllers;

[Authorize]
public class EnviosController : Controller
{
    private readonly EnviosApiClient _enviosApi;

    public EnviosController(EnviosApiClient enviosApi)
    {
        _enviosApi = enviosApi;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(usuarioId) || !int.TryParse(usuarioId, out var id))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        var envios = await _enviosApi.ObtenerMiosAsync(id);
        return View(envios);
    }

    [HttpGet]
    public IActionResult Nuevo()
    {
        return View(new NuevoEnvioViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nuevo(NuevoEnvioViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(usuarioId) || !int.TryParse(usuarioId, out var id))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        var resultado = await _enviosApi.CrearAsync(
            id,
            modelo.OrigenCalle,
            modelo.OrigenAltura,
            modelo.OrigenPiso,
            modelo.OrigenDepartamento,
            modelo.OrigenLocalidad,
            modelo.OrigenProvincia,
            modelo.DestinoCalle,
            modelo.DestinoAltura,
            modelo.DestinoPiso,
            modelo.DestinoDepartamento,
            modelo.DestinoLocalidad,
            modelo.DestinoProvincia,
            modelo.FechaLimite,
            modelo.PesoKg,
            modelo.Tamanio,
            modelo.Descripcion,
            modelo.Monto
        );

        if (resultado.Error is not null)
        {
            ModelState.AddModelError(string.Empty, resultado.Error);
            return View(modelo);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(usuarioId) || !int.TryParse(usuarioId, out var usuario))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        var envio = await _enviosApi.ObtenerPorIdAsync(id, usuario);
        if (envio is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var modelo = new NuevoEnvioViewModel
        {
            OrigenCalle = envio.OrigenCalle,
            OrigenAltura = envio.OrigenAltura,
            OrigenPiso = envio.OrigenPiso,
            OrigenDepartamento = envio.OrigenDepartamento,
            OrigenLocalidad = envio.OrigenLocalidad,
            OrigenProvincia = envio.OrigenProvincia,
            OrigenLat = envio.OrigenLat,
            OrigenLng = envio.OrigenLng,
            DestinoCalle = envio.DestinoCalle,
            DestinoAltura = envio.DestinoAltura,
            DestinoPiso = envio.DestinoPiso,
            DestinoDepartamento = envio.DestinoDepartamento,
            DestinoLocalidad = envio.DestinoLocalidad,
            DestinoProvincia = envio.DestinoProvincia,
            DestinoLat = envio.DestinoLat,
            DestinoLng = envio.DestinoLng,
            FechaLimite = envio.FechaLimite,
            PesoKg = envio.PesoKg,
            Tamanio = envio.Tamanio,
            Descripcion = envio.Descripcion,
            Monto = envio.Monto,
        };

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, NuevoEnvioViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(usuarioId) || !int.TryParse(usuarioId, out var usuario))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        var resultado = await _enviosApi.EditarAsync(
            id,
            usuario,
            modelo.OrigenCalle,
            modelo.OrigenAltura,
            modelo.OrigenPiso,
            modelo.OrigenDepartamento,
            modelo.OrigenLocalidad,
            modelo.OrigenProvincia,
            modelo.DestinoCalle,
            modelo.DestinoAltura,
            modelo.DestinoPiso,
            modelo.DestinoDepartamento,
            modelo.DestinoLocalidad,
            modelo.DestinoProvincia,
            modelo.FechaLimite,
            modelo.PesoKg,
            modelo.Tamanio,
            modelo.Descripcion,
            modelo.Monto
        );

        if (resultado.Error is not null)
        {
            ModelState.AddModelError(string.Empty, resultado.Error);
            return View(modelo);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrar(int id)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(usuarioId) || !int.TryParse(usuarioId, out var usuario))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        await _enviosApi.BorrarAsync(id, usuario);
        return RedirectToAction(nameof(Index));
    }
}

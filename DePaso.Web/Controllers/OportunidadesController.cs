using System.Security.Claims;
using DePaso.Logica.dtos;
using DePaso.Web.Models;
using DePaso.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DePaso.Web.Controllers;

[Authorize]
[CulturaInvariante]
public class OportunidadesController : Controller
{
    private const string MensajeSinApi = "No se pudo conectar con la API. Verifica que DePaso.Api este corriendo.";
    private const string MensajeAceptado = "Envío aceptado. Lo ves en Mis transportes.";
    private const string MensajeAceptados = "Envíos aceptados. Los ves en Mis transportes.";
    private const string MensajeRecalcular = "Otro conductor ya tomó un envío. Recalculá la combinación.";

    private readonly OportunidadesApiClient _oportunidades;

    public OportunidadesController(OportunidadesApiClient oportunidades)
    {
        _oportunidades = oportunidades;
    }

    [HttpGet("/Oportunidades")]
    public async Task<IActionResult> Oportunidades()
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        try
        {
            return View(await _oportunidades.ObtenerSalidasAsync(usuarioId));
        }
        catch (HttpRequestException)
        {
            ViewData["Error"] = MensajeSinApi;
            return View(new List<OportunidadSalidaDto>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Lista(int viajeId, DateOnly fecha, string? orden)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        ListaCoincidenciasDto? lista;
        try
        {
            lista = await _oportunidades.ListarAsync(viajeId, usuarioId, fecha);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = MensajeSinApi;
            return RedirectToAction(nameof(Oportunidades));
        }

        if (lista is null)
        {
            TempData["Error"] = "No encontramos esa salida.";
            return RedirectToAction(nameof(Oportunidades));
        }

        orden = NormalizarOrden(orden);
        ViewData["Orden"] = orden;
        return View(lista with { Coincidencias = Ordenar(lista.Coincidencias, orden) });
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int viajeId, int envioId, DateOnly fecha)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        DetalleCoincidenciaDto? detalle;
        try
        {
            detalle = await _oportunidades.DetalleAsync(viajeId, envioId, usuarioId, fecha);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = MensajeSinApi;
            return RedirectToAction(nameof(Lista), new { viajeId, fecha = Fecha(fecha) });
        }

        if (detalle is null)
        {
            TempData["Error"] = "No encontramos ese envío para esta salida.";
            return RedirectToAction(nameof(Lista), new { viajeId, fecha = Fecha(fecha) });
        }

        ViewData["ViajeId"] = viajeId;
        ViewData["Fecha"] = fecha;
        return View(detalle);
    }

    [HttpGet]
    public async Task<IActionResult> Optimizar(int viajeId, DateOnly fecha)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        OptimizacionCoincidenciasDto? optimizacion;
        try
        {
            optimizacion = await _oportunidades.OptimizarAsync(viajeId, usuarioId, fecha);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = MensajeSinApi;
            return RedirectToAction(nameof(Oportunidades));
        }

        if (optimizacion is null)
        {
            TempData["Error"] = "No encontramos esa salida.";
            return RedirectToAction(nameof(Oportunidades));
        }

        ViewData["ViajeId"] = viajeId;
        ViewData["Fecha"] = fecha;
        return View(optimizacion);
    }

    [HttpGet]
    public async Task<IActionResult> Resumen(int viajeId, DateOnly fecha, List<int> envioIds)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        if (envioIds.Count == 0)
        {
            TempData["Error"] = "Elegí al menos un envío para la combinación.";
            return RedirectToAction(nameof(Optimizar), new { viajeId, fecha = Fecha(fecha) });
        }

        ListaCoincidenciasDto? lista;
        try
        {
            lista = await _oportunidades.ListarAsync(viajeId, usuarioId, fecha);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = MensajeSinApi;
            return RedirectToAction(nameof(Oportunidades));
        }

        if (lista is null)
        {
            TempData["Error"] = "No encontramos esa salida.";
            return RedirectToAction(nameof(Oportunidades));
        }

        var items = lista.Coincidencias.Where(c => envioIds.Contains(c.EnvioId)).ToList();
        var noDisponibles = envioIds.Except(items.Select(c => c.EnvioId)).ToList();
        return View(new ResumenCoincidenciasViewModel(viajeId, lista.Viaje, fecha, items, noDisponibles));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aceptar(int viajeId, DateOnly fecha, int envioId, string? volverA)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        var resultado = await ReservarAsync(viajeId, usuarioId, fecha, new[] { envioId });
        if (resultado is null)
        {
            return RedirectToAction(nameof(Lista), new { viajeId, fecha = Fecha(fecha) });
        }

        if (resultado.Aceptados.Count > 0)
        {
            TempData["Mensaje"] = MensajeAceptado;
        }
        else if (resultado.Tomados.Count > 0)
        {
            TempData["Error"] = resultado.Tomados[0].Motivo + " Recargá la lista.";
        }
        else
        {
            var motivo = resultado.NoEntran.FirstOrDefault()?.Motivo ?? "El envío ya no entra en la salida.";
            TempData["Error"] = motivo.Contains("Supera") || motivo.Contains("No quedan")
                ? "No se puede aceptar: supera tus límites. " + motivo
                : "No se puede aceptar. " + motivo;
        }

        return volverA == "optimizar"
            ? RedirectToAction(nameof(Optimizar), new { viajeId, fecha = Fecha(fecha) })
            : RedirectToAction(nameof(Lista), new { viajeId, fecha = Fecha(fecha) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarReserva(int viajeId, DateOnly fecha, List<int> envioIds)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
        {
            return RedirectToAction("Ingresar", "Cuenta");
        }

        if (envioIds.Count == 0)
        {
            TempData["Error"] = "Elegí al menos un envío para la combinación.";
            return RedirectToAction(nameof(Optimizar), new { viajeId, fecha = Fecha(fecha) });
        }

        var resultado = await ReservarAsync(viajeId, usuarioId, fecha, envioIds);
        if (resultado is null)
        {
            return RedirectToAction(nameof(Lista), new { viajeId, fecha = Fecha(fecha) });
        }

        if (resultado.Tomados.Count > 0)
        {
            if (resultado.Aceptados.Count > 0)
            {
                TempData["Mensaje"] = MensajeAceptados;
            }
            TempData["Error"] = MensajeRecalcular;
            return RedirectToAction(nameof(Optimizar), new { viajeId, fecha = Fecha(fecha) });
        }

        if (resultado.Aceptados.Count > 0)
        {
            TempData["Mensaje"] = resultado.Aceptados.Count == 1 ? MensajeAceptado : MensajeAceptados;
        }
        if (resultado.NoEntran.Count > 0)
        {
            TempData["Error"] = "No se pudieron reservar: " + string.Join(" ", resultado.NoEntran.Select(f => f.Motivo));
        }

        return RedirectToAction(nameof(Lista), new { viajeId, fecha = Fecha(fecha) });
    }

    /// <summary>Llama a la API y deja el error en TempData. Devuelve null si falló.</summary>
    private async Task<ResultadoAceptacionDto?> ReservarAsync(
        int viajeId,
        int usuarioId,
        DateOnly fecha,
        IReadOnlyList<int> envioIds
    )
    {
        try
        {
            var (resultado, error) = await _oportunidades.AceptarAsync(viajeId, usuarioId, fecha, envioIds);
            if (error is not null)
            {
                TempData["Error"] = error;
                return null;
            }
            return resultado;
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = MensajeSinApi;
            return null;
        }
    }

    private static string NormalizarOrden(string? orden) =>
        orden switch
        {
            "desvio" => "desvio",
            "compatibilidad" => "compatibilidad",
            _ => "ganancia",
        };

    private static IReadOnlyList<CoincidenciaDto> Ordenar(IReadOnlyList<CoincidenciaDto> items, string orden) =>
        orden switch
        {
            "desvio" => items.OrderBy(c => c.DesvioKm).ThenByDescending(c => c.Pago).ThenBy(c => c.EnvioId).ToList(),
            "compatibilidad" => items.OrderBy(c => c.Nivel).ThenBy(c => c.DesvioKm).ThenByDescending(c => c.Pago).ThenBy(c => c.EnvioId).ToList(),
            _ => items.OrderByDescending(c => c.Pago).ThenBy(c => c.DesvioKm).ThenBy(c => c.EnvioId).ToList(),
        };

    private static string Fecha(DateOnly fecha) => fecha.ToString("yyyy-MM-dd");

    private bool TryObtenerUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}

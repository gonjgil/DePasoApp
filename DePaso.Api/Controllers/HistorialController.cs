using DePaso.Logica.Dtos;
using DePaso.Logica.servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DePaso.Api.Controllers;

[ApiController]
[Route("api/historial")]
[Tags("Historial")]
[Produces("application/json")]
public class HistorialController : ControllerBase
{
    private readonly IHistorialServicio _historial;

    public HistorialController(IHistorialServicio historial)
    {
        _historial = historial ?? throw new ArgumentNullException(nameof(historial));
    }

    [HttpGet]
    [EndpointSummary("Historial de envíos entregados")]
    [EndpointDescription("Operaciones entregadas del conductor, agrupadas por viaje y fecha, con los totales.")]
    [ProducesResponseType<HistorialDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener([FromQuery, BindRequired] int usuarioId, CancellationToken ct)
    {
        var historial = await _historial.ObtenerAsync(usuarioId, ct);
        if (historial is null)
        {
            return NotFound(new { mensaje = "Usuario no encontrado." });
        }

        return Ok(historial);
    }
}

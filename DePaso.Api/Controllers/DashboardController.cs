using DePaso.Logica.Dtos;
using DePaso.Logica.servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DePaso.Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Tags("Dashboard")]
    [Produces("application/json")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardServicio _dashboard;

        public DashboardController(IDashboardServicio dashboard)
        {
            _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
        }

        [HttpGet]
        [EndpointSummary("Resumen del dashboard")]
        [EndpointDescription("Resumen de viajes, envíos pendientes y oportunidades para el usuario indicado.")]
        [ProducesResponseType<DashboardResumenDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Resumen(
            [FromQuery, BindRequired] int usuarioId,
            CancellationToken cancelacion = default)
        {
            var resumen = await _dashboard.ObtenerResumenAsync(usuarioId, cancelacion);
            if (resumen is null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }

            return Ok(resumen);
        }

        [HttpGet("potencial")]
        [EndpointSummary("Potencial mensual del dashboard")]
        [EndpointDescription("Detalle del potencial mensual optimizado según el criterio indicado.")]
        [ProducesResponseType<PotencialMesDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PotencialMensual(
            [FromQuery, BindRequired] int usuarioId,
            [FromQuery] string criterio = "ganancia",
            CancellationToken cancelacion = default)
        {
            var potencial = await _dashboard.ObtenerPotencialMesAsync(
                usuarioId,
                criterio,
                cancelacion
            );

            if (potencial is null)
            {
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }

            return Ok(potencial);
        }
    }
}

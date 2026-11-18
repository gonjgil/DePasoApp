using DePaso.Logica.dtos;
using DePaso.Logica.servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DePaso.Api.Controllers;

[ApiController]
[Route("api/transportes")]
[Tags("Transportes")]
[Produces("application/json")]
public class TransportesController : ControllerBase
{
    private readonly ITransportesServicio _transportes;

    public TransportesController(ITransportesServicio transportes)
    {
        _transportes = transportes ?? throw new ArgumentNullException(nameof(transportes));
    }

    [HttpGet]
    [EndpointSummary("Envíos aceptados por el conductor")]
    [EndpointDescription("Mis transportes: cada asignación con su estado actual y el siguiente paso posible.")]
    [ProducesResponseType<IReadOnlyList<TransporteDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery, BindRequired] int usuarioId, CancellationToken ct) =>
        Ok(await _transportes.ListarAsync(usuarioId, ct));

    [HttpPatch("{asignacionId:int}/avanzar")]
    [EndpointSummary("Pasa el envío al siguiente estado")]
    [EndpointDescription("Orden: Conductor asignado → Retirado → En camino → Entregado. estadoEsperado es el estado que mostraba la pantalla; si cambió, responde 409.")]
    [ProducesResponseType<TransporteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Avanzar(
        int asignacionId,
        [FromQuery, BindRequired] int usuarioId,
        [FromQuery, BindRequired] byte estadoEsperado,
        CancellationToken ct) =>
        Responder(await _transportes.AvanzarAsync(asignacionId, usuarioId, estadoEsperado, ct), v => Ok(v));

    [HttpPatch("{asignacionId:int}/cancelar")]
    [EndpointSummary("Cancela la asignación y devuelve el envío a Oportunidades")]
    [EndpointDescription("Sólo mientras el envío esté en Conductor asignado. estadoEsperado es el estado que mostraba la pantalla; si cambió, responde 409.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(
        int asignacionId,
        [FromQuery, BindRequired] int usuarioId,
        [FromQuery, BindRequired] byte estadoEsperado,
        CancellationToken ct) =>
        Responder(await _transportes.CancelarAsync(asignacionId, usuarioId, estadoEsperado, ct), _ => NoContent());

    private IActionResult Responder<T>(Resultado<T> resultado, Func<T, IActionResult> exito)
    {
        if (resultado.Exito)
        {
            return exito(resultado.Valor!);
        }

        var cuerpo = new ErrorViajeRespuesta(resultado.Mensaje ?? "Error.", resultado.Errores);
        return resultado.Error switch
        {
            TipoError.Validacion => BadRequest(cuerpo),
            TipoError.NoEncontrado => NotFound(cuerpo),
            TipoError.Prohibido => StatusCode(StatusCodes.Status403Forbidden, cuerpo),
            TipoError.Conflicto => Conflict(cuerpo),
            _ => StatusCode(StatusCodes.Status500InternalServerError, cuerpo),
        };
    }
}

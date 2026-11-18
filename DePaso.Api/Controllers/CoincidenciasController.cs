using DePaso.Api.Models;
using DePaso.Logica.dtos;
using DePaso.Logica.servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DePaso.Api.Controllers;

[ApiController]
[Route("api/viajes/{viajeId:int}/coincidencias")]
[Tags("Coincidencias")]
[Produces("application/json")]
public class CoincidenciasController : ControllerBase
{
    private readonly IServicioCoincidencias _coincidencias;

    public CoincidenciasController(IServicioCoincidencias coincidencias)
    {
        _coincidencias = coincidencias ?? throw new ArgumentNullException(nameof(coincidencias));
    }

    [HttpGet]
    [EndpointSummary("Envíos que coinciden con una salida")]
    [EndpointDescription("Envíos de otras personas en Buscando conductor que entran en el peso y el desvío que quedan en la salida.")]
    [ProducesResponseType<ListaCoincidenciasDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Listar(
        int viajeId,
        [FromQuery, BindRequired] int usuarioId,
        [FromQuery, BindRequired] DateOnly fecha,
        CancellationToken ct) =>
        Responder(await _coincidencias.ListarAsync(viajeId, usuarioId, fecha, ct), v => Ok(v));

    [HttpGet("{envioId:int}")]
    [EndpointSummary("Detalle de un envío para una salida")]
    [EndpointDescription("Compara la ruta habitual con la ruta que incluye el envío, y dice si se puede aceptar y por qué no si supera los límites.")]
    [ProducesResponseType<DetalleCoincidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Detalle(
        int viajeId,
        int envioId,
        [FromQuery, BindRequired] int usuarioId,
        [FromQuery, BindRequired] DateOnly fecha,
        CancellationToken ct) =>
        Responder(await _coincidencias.DetalleAsync(viajeId, envioId, usuarioId, fecha, ct), v => Ok(v));

    [HttpGet("optimizar")]
    [EndpointSummary("Mejor combinación de envíos para una salida")]
    [EndpointDescription("Elige la combinación de mayor pago que no supera los topes del viaje; ante empate de pago gana la de menor desvío.")]
    [ProducesResponseType<OptimizacionCoincidenciasDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Optimizar(
        int viajeId,
        [FromQuery, BindRequired] int usuarioId,
        [FromQuery, BindRequired] DateOnly fecha,
        CancellationToken ct) =>
        Responder(await _coincidencias.OptimizarAsync(viajeId, usuarioId, fecha, ct), v => Ok(v));

    [HttpPost("aceptar")]
    [EndpointSummary("Reserva envíos para una salida")]
    [EndpointDescription("Reserva los que siguen libres y entran en la capacidad de la salida. Los tomados por otro conductor o que ya no entran vuelven en la respuesta con su motivo.")]
    [ProducesResponseType<ResultadoAceptacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Aceptar(
        int viajeId,
        [FromBody] AceptarCoincidenciasModelo modelo,
        CancellationToken ct) =>
        Responder(
            await _coincidencias.AceptarAsync(viajeId, modelo.UsuarioId, modelo.Fecha, modelo.EnvioIds, ct),
            v => Ok(v));

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

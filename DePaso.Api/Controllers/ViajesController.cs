using DePaso.Api.Models;
using DePaso.Logica.dtos;
using DePaso.Logica.servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DePaso.Api.Controllers;

public sealed record ErrorViajeRespuesta(string Mensaje, IReadOnlyList<string> Errores);

[ApiController]
[Route("api/viajes")]
[Tags("Viajes")]
[Produces("application/json")]
public class ViajesController : ControllerBase
{
    private readonly IViajeServicio _viajes;

    public ViajesController(IViajeServicio viajes)
    {
        _viajes = viajes ?? throw new ArgumentNullException(nameof(viajes));
    }

    [HttpGet]
    [EndpointSummary("Lista los viajes del usuario")]
    [EndpointDescription("Devuelve sólo los viajes creados por el usuario. Filtro estado: activos | inactivos | todos (por defecto).")]
    [ProducesResponseType<IReadOnlyList<ViajeResumenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery, BindRequired] int usuarioId,
        [FromQuery] string? estado,
        CancellationToken ct)
    {
        if (!TryLeerFiltro(estado, out var filtro))
        {
            return BadRequest(Error("El filtro de estado tiene que ser 'activos', 'inactivos' o 'todos'."));
        }

        return Ok(await _viajes.ListarAsync(usuarioId, filtro, ct));
    }

    [HttpGet("{id:int}", Name = "ObtenerViaje")]
    [EndpointSummary("Detalle de un viaje")]
    [EndpointDescription("Incluye recorrido, paradas y paquetes asignados. Sólo accesible para el dueño del viaje.")]
    [ProducesResponseType<ViajeDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, [FromQuery, BindRequired] int usuarioId, CancellationToken ct) =>
        Responder(await _viajes.ObtenerAsync(id, usuarioId, ct), v => Ok(v));

    [HttpPost]
    [EndpointSummary("Registra un viaje habitual")]
    [EndpointDescription("Valida los datos, geocodifica las direcciones sin coordenadas, calcula la ruta y guarda el viaje como Activo.")]
    [ProducesResponseType<ViajeDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Crear([FromBody] ViajeAlta modelo, CancellationToken ct) =>
        Responder(
            await _viajes.CrearAsync(modelo.ADatos(), ct),
            v => CreatedAtRoute("ObtenerViaje", new { id = v.Id, usuarioId = v.UsuarioId }, v));

    [HttpPut("{id:int}")]
    [EndpointSummary("Modifica un viaje")]
    [EndpointDescription(
        "Reemplaza los datos del viaje (incluidas las paradas). Si hay envíos asignados en curso no se puede cambiar "
            + "el recorrido ni los días, ni bajar la capacidad por debajo de lo asignado (409).")]
    [ProducesResponseType<ViajeDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ViajeAlta modelo, CancellationToken ct) =>
        Responder(await _viajes.ActualizarAsync(id, modelo.ADatos(), ct), v => Ok(v));

    [HttpPatch("{id:int}/desactivar")]
    [EndpointSummary("Desactiva un viaje")]
    [EndpointDescription("Pasa el viaje a Inactivo. Devuelve 409 si ya estaba inactivo o si tiene envíos asignados en curso.")]
    [ProducesResponseType<ViajeDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Desactivar(int id, [FromQuery, BindRequired] int usuarioId, CancellationToken ct) =>
        Responder(await _viajes.DesactivarAsync(id, usuarioId, ct), v => Ok(v));

    [HttpPatch("{id:int}/activar")]
    [EndpointSummary("Vuelve a activar un viaje")]
    [ProducesResponseType<ViajeDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activar(int id, [FromQuery, BindRequired] int usuarioId, CancellationToken ct) =>
        Responder(await _viajes.ActivarAsync(id, usuarioId, ct), v => Ok(v));

    [HttpPost("ruta")]
    [EndpointSummary("Calcula la ruta sin guardar")]
    [EndpointDescription("Para el botón 'Calcular/actualizar ruta': devuelve distancia, duración y coordenadas de cada punto.")]
    [ProducesResponseType<RutaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalcularRuta([FromBody] CalcularRutaModelo modelo, CancellationToken ct) =>
        Responder(
            await _viajes.CalcularRutaAsync(
                modelo.Origen.ADatos(),
                modelo.Destino.ADatos(),
                (modelo.Paradas ?? new()).Select(p => p.AParada()).ToList(),
                ct),
            v => Ok(v));

    [HttpGet("{id:int}/paquetes")]
    [EndpointSummary("Paquetes asignados a un viaje")]
    [ProducesResponseType<IReadOnlyList<PaqueteAsignadoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Paquetes(int id, [FromQuery, BindRequired] int usuarioId, CancellationToken ct) =>
        Responder(await _viajes.ListarPaquetesAsync(id, usuarioId, ct), v => Ok(v));

    [HttpGet("{id:int}/paradas")]
    [EndpointSummary("Paradas de un viaje, en orden")]
    [ProducesResponseType<IReadOnlyList<ParadaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarParadas(int id, [FromQuery, BindRequired] int usuarioId, CancellationToken ct) =>
        Responder(await _viajes.ListarParadasAsync(id, usuarioId, ct), v => Ok(v));

    [HttpPost("{id:int}/paradas")]
    [EndpointSummary("Agrega una parada")]
    [EndpointDescription("Si se indica Orden se inserta en esa posición; si no, al final. Recalcula la ruta.")]
    [ProducesResponseType<ViajeDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AgregarParada(int id, [FromBody] ParadaAlta modelo, CancellationToken ct) =>
        Responder(
            await _viajes.AgregarParadaAsync(id, modelo.UsuarioId, modelo.AParada(), ct),
            v => CreatedAtRoute("ObtenerViaje", new { id = v.Id, usuarioId = v.UsuarioId }, v));

    [HttpPut("{id:int}/paradas/orden")]
    [EndpointSummary("Reordena las paradas")]
    [EndpointDescription("Recibe los ids de todas las paradas del viaje en el nuevo orden. Recalcula la ruta.")]
    [ProducesResponseType<ViajeDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReordenarParadas(int id, [FromBody] ReordenarParadas modelo, CancellationToken ct) =>
        Responder(await _viajes.ReordenarParadasAsync(id, modelo.UsuarioId, modelo.ParadaIds, ct), v => Ok(v));

    [HttpPut("{id:int}/paradas/{paradaId:int}")]
    [EndpointSummary("Modifica una parada")]
    [ProducesResponseType<ViajeDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ModificarParada(
        int id,
        int paradaId,
        [FromBody] ParadaModificacion modelo,
        CancellationToken ct) =>
        Responder(await _viajes.ModificarParadaAsync(id, paradaId, modelo.UsuarioId, modelo.ADatos(), ct), v => Ok(v));

    [HttpDelete("{id:int}/paradas/{paradaId:int}")]
    [EndpointSummary("Elimina una parada")]
    [EndpointDescription("Las paradas restantes se renumeran y se recalcula la ruta.")]
    [ProducesResponseType<ViajeDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorViajeRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarParada(
        int id,
        int paradaId,
        [FromQuery, BindRequired] int usuarioId,
        CancellationToken ct) =>
        Responder(await _viajes.EliminarParadaAsync(id, paradaId, usuarioId, ct), v => Ok(v));


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

    private static ErrorViajeRespuesta Error(string mensaje) => new(mensaje, new[] { mensaje });

    public static bool TryLeerFiltro(string? estado, out FiltroViajes filtro)
    {
        switch (estado?.Trim().ToLowerInvariant())
        {
            case null or "" or "todos":
                filtro = FiltroViajes.Todos;
                return true;
            case "activos" or "activo":
                filtro = FiltroViajes.Activos;
                return true;
            case "inactivos" or "inactivo":
                filtro = FiltroViajes.Inactivos;
                return true;
            default:
                filtro = FiltroViajes.Todos;
                return false;
        }
    }
}

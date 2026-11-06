using DePaso.Api.Models;
using DePaso.Logica;
using Microsoft.AspNetCore.Mvc;

namespace DePaso.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnviosController : Controller
{
    private readonly EnvioServicio _envios;

    public EnviosController(EnvioServicio envios)
    {
        _envios = envios ?? throw new ArgumentNullException(nameof(envios));
    }

    [HttpGet("mios")]
    public async Task<IActionResult> MisEnvios([FromQuery] int usuarioId)
    {
        var envios = await _envios.ObtenerPorUsuario(usuarioId);

        return Ok(
            envios.Select(e => new
            {
                e.Id,
                e.UsuarioId,
                e.OrigenDir,
                e.OrigenLat,
                e.OrigenLng,
                e.DestinoDir,
                e.DestinoLat,
                e.DestinoLng,
                e.FechaLimite,
                e.PesoKg,
                e.Tamanio,
                e.Descripcion,
                e.Monto,
                e.Estado,
                e.CreadoEn,
            })
        );
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id, [FromQuery] int usuarioId)
    {
        var envio = await _envios.ObtenerPorId(id, usuarioId);

        if (envio is null)
        {
            return NotFound(new { mensaje = "Envío no encontrado." });
        }

        return Ok(
            new
            {
                envio.Id,
                envio.UsuarioId,
                envio.OrigenDir,
                envio.OrigenLat,
                envio.OrigenLng,
                envio.DestinoDir,
                envio.DestinoLat,
                envio.DestinoLng,
                envio.FechaLimite,
                envio.PesoKg,
                envio.Tamanio,
                envio.Descripcion,
                envio.Monto,
                envio.Estado,
                envio.CreadoEn,
            }
        );
    }

    [HttpPost("crear")]
    public async Task<IActionResult> Crear([FromBody] EnvioRegistro modelo)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var envio = await _envios.Crear(
            modelo.UsuarioId,
            modelo.OrigenDir,
            modelo.OrigenLat,
            modelo.OrigenLng,
            modelo.DestinoDir,
            modelo.DestinoLat,
            modelo.DestinoLng,
            modelo.FechaLimite,
            modelo.PesoKg,
            modelo.Tamanio,
            modelo.Descripcion,
            modelo.Monto
        );

        if (envio is null)
        {
            return NotFound(new { mensaje = "Usuario no encontrado." });
        }

        return Created(
            string.Empty,
            new
            {
                envio.Id,
                envio.UsuarioId,
                envio.OrigenDir,
                envio.OrigenLat,
                envio.OrigenLng,
                envio.DestinoDir,
                envio.DestinoLat,
                envio.DestinoLng,
                envio.FechaLimite,
                envio.PesoKg,
                envio.Tamanio,
                envio.Descripcion,
                envio.Monto,
                envio.Estado,
                envio.CreadoEn,
            }
        );
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EnvioRegistro modelo)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var envio = await _envios.Editar(
            id,
            modelo.UsuarioId,
            modelo.OrigenDir,
            modelo.OrigenLat,
            modelo.OrigenLng,
            modelo.DestinoDir,
            modelo.DestinoLat,
            modelo.DestinoLng,
            modelo.FechaLimite,
            modelo.PesoKg,
            modelo.Tamanio,
            modelo.Descripcion,
            modelo.Monto
        );

        if (envio is null)
        {
            return NotFound(new { mensaje = "Envío no encontrado." });
        }

        return Ok(
            new
            {
                envio.Id,
                envio.UsuarioId,
                envio.OrigenDir,
                envio.OrigenLat,
                envio.OrigenLng,
                envio.DestinoDir,
                envio.DestinoLat,
                envio.DestinoLng,
                envio.FechaLimite,
                envio.PesoKg,
                envio.Tamanio,
                envio.Descripcion,
                envio.Monto,
                envio.Estado,
                envio.CreadoEn,
            }
        );
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Borrar(int id, [FromQuery] int usuarioId)
    {
        var eliminado = await _envios.Borrar(id, usuarioId);

        if (!eliminado)
        {
            return NotFound(new { mensaje = "Envío no encontrado." });
        }

        return Ok(new { mensaje = "Envío eliminado." });
    }
}

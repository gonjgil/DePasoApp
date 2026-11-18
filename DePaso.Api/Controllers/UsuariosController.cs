using DePaso.Api.Models;
using DePaso.Entidades;
using DePaso.Logica;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DePaso.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Usuarios")]
[Produces("application/json")]
/// <summary>
/// Expone el ingreso y el registro de usuarios en la aplicación.
/// </summary>
public class UsuariosController : Controller
{
    private readonly UsuarioServicio Usuarios;

    public UsuariosController(UsuarioServicio usuarios)
    {
        Usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
    }

    [HttpPost("ingresar")]
    [EndpointSummary("Inicia sesión")]
    [EndpointDescription("Valida el email y la contraseña y devuelve los datos básicos del usuario autenticado.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Ingresar([FromBody] UsuarioLogin modelo)
    {
        var usuario = await Usuarios.Ingresar(modelo.Email, modelo.Password);
        if (usuario is null)
        {
            return Unauthorized(new { mensaje = "Email o Password incorrectos." });
        }
        return Ok(
            new
            {
                usuario.Id,
                usuario.Nombre,
                usuario.Apellido,
                usuario.Email,
            }
        );
    }

    [HttpPost("registrarse")]
    [EndpointSummary("Registra un usuario")]
    [EndpointDescription("Valida los datos, crea la cuenta con la contraseña protegida y devuelve el usuario creado.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrarse([FromBody] UsuarioRegistro modelo)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (usuario, error) = await Usuarios.Registrar(
            modelo.Nombre,
            modelo.Apellido,
            modelo.Email,
            modelo.Password
        );

        if (error is not null)
        {
            return Conflict(new { mensaje = error });
        }
        return Created(
            string.Empty,
            new
            {
                usuario!.Id,
                usuario.Nombre,
                usuario.Apellido,
                usuario.Email,
                usuario.CreadoEn,
            }
        );
    }
}

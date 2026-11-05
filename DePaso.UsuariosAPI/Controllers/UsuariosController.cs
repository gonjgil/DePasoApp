using DePaso.Entidades;
using DePaso.Logica;
using DePaso.UsuariosAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace DePaso.UsuariosAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : Controller
{
    private readonly UsuarioServicio Usuarios;

    public UsuariosController(UsuarioServicio usuarios)
    {
        Usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
    }

    [HttpPost("ingresar")]
    public async Task<IActionResult> Ingresar([FromBody] UsuarioLogin modelo)
    {
        var usuario = await Usuarios.Ingresar(modelo.Email, modelo.Password);
        if (usuario is null)
        {
            return Unauthorized(new
            {
                mensaje = "Email o Password incorrectos."
            });
        }
        return Ok(new
        {
            usuario.Id,
            usuario.Nombre,
            usuario.Apellido,
            usuario.Email
        });
    }

    [HttpPost("registrarse")]
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
            modelo.Password);

        if (error is not null)
        {
            return Conflict(new
            {
                mensaje = error
            });
        }
        return Created(string.Empty, new
        {
            usuario!.Id,
            usuario.Nombre,
            usuario.Apellido,
            usuario.Email,
            usuario.CreadoEn
        });
    }
}
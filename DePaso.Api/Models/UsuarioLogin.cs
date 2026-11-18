namespace DePaso.Api.Models;

/// <summary>Datos necesarios para iniciar sesión.</summary>
public class UsuarioLogin
{
    /// <summary>Email con el que el usuario fue registrado.</summary>
    public required string Email { get; set; }

    /// <summary>Contraseña del usuario.</summary>
    public required string Password { get; set; }
}

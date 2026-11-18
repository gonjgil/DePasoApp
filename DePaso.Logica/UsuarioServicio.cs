using System.Security.Cryptography;
using DePaso.Entidades;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Logica;

/// <summary>
/// Gestiona el registro y la autenticación de usuarios mediante hashes PBKDF2 con salt aleatorio.
/// </summary>
public class UsuarioServicio
{
    private readonly DePasoAppContext _context;

    public UsuarioServicio(DePasoAppContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Registra un usuario nuevo si el email aún no existe.
    /// </summary>
    /// <returns>El usuario creado y un error vacío, o usuario nulo y un mensaje de conflicto.</returns>
    public async Task<(Usuarios? Usuario, string? Error)> Registrar(string nombre, string apellido, string email, string contrasena)
    {
        var correo = email.Trim();
        var existe = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == correo.ToLower());
        if (existe)
        {
            return (null, "Ese email ya está registrado.");
        }

        var usuario = new Usuarios
        {
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            Email = correo,
            PasswordHash = Hash(contrasena),
            CreadoEn = DateTime.Now
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return (usuario, null);
    }

    /// <summary>
    /// Valida las credenciales de un usuario por email y contraseña.
    /// </summary>
    /// <returns>El usuario encontrado o <see langword="null"/> si las credenciales son incorrectas.</returns>
    public async Task<Usuarios?> Ingresar(string email, string contrasena)
    {
        var correo = email.Trim();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == correo.ToLower());
        if (usuario == null || !Verificar(usuario.PasswordHash, contrasena))
        {
            return null;
        }

        return usuario;
    }

    private static string Hash(string contrasena)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, salt, 100000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool Verificar(string guardado, string contrasena)
    {
        var partes = guardado.Split('.');
        if (partes.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(partes[0]);
        var hash = Convert.FromBase64String(partes[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(contrasena, salt, 100000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, actual);
    }
}

using System.ComponentModel.DataAnnotations;

namespace DePaso.Web.Models;

public class RegistrarseViewModel
{
    [Required(ErrorMessage = "Requerido.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Requerido.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "Ingresá un email válido.")]
    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Mínimo 8 caracteres.")]
    [MinLength(8, ErrorMessage = "Mínimo 8 caracteres.")]
    [DataType(DataType.Password)]
    public string Contrasena { get; set; } = "";

    [Required(ErrorMessage = "Las contraseñas no coinciden.")]
    [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmarContrasena { get; set; } = "";
}

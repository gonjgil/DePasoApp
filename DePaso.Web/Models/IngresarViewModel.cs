using System.ComponentModel.DataAnnotations;

namespace DePaso.Web.Models;

public class IngresarViewModel
{
    [Required(ErrorMessage = "Ingresá un email válido.")]
    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    [DataType(DataType.Password)]
    public string Contrasena { get; set; } = "";
}

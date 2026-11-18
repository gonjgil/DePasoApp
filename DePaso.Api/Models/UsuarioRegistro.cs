using System.ComponentModel.DataAnnotations;

namespace DePaso.Api.Models
{
    /// <summary>Datos necesarios para registrar una cuenta.</summary>
    public class UsuarioRegistro
    {
        /// <summary>Nombre del usuario.</summary>
        [Required(ErrorMessage = "Requerido.")]
        public string Nombre { get; set; } = "";

        /// <summary>Apellido del usuario.</summary>
        [Required(ErrorMessage = "Requerido.")]
        public string Apellido { get; set; } = "";

        /// <summary>Email único que identificará la cuenta.</summary>
        [Required(ErrorMessage = "Ingresá un email válido.")]
        [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
        public string Email { get; set; } = "";

        /// <summary>Contraseña con al menos 8 caracteres.</summary>
        [Required(ErrorMessage = "Mínimo 8 caracteres.")]
        [MinLength(8, ErrorMessage = "Mínimo 8 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}

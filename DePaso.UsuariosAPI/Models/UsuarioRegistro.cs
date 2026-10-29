using System.ComponentModel.DataAnnotations;

namespace DePaso.UsuariosAPI.Models
{
    public class UsuarioRegistro
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
        public string Password { get; set; } = "";
        
    }
}

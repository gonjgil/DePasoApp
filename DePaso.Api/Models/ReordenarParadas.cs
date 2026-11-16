using System.ComponentModel.DataAnnotations;

namespace DePaso.Api.Models;

public class ReordenarParadas
{
    [Range(1, int.MaxValue, ErrorMessage = "Falta el usuario.")]
    public int UsuarioId { get; set; }

    [Required]
    public List<int> ParadaIds { get; set; } = new();
}

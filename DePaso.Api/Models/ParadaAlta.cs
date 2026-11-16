using System.ComponentModel.DataAnnotations;

namespace DePaso.Api.Models;

public class ParadaAlta : ParadaViaje
{
    [Range(1, int.MaxValue, ErrorMessage = "Falta el usuario.")]
    public int UsuarioId { get; set; }
}
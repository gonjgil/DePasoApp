using System.ComponentModel.DataAnnotations;

namespace DePaso.Api.Models;

public class ParadaModificacion : DireccionViaje
{
    [Range(1, int.MaxValue, ErrorMessage = "Falta el usuario.")]
    public int UsuarioId { get; set; }
}
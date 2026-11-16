using System.ComponentModel.DataAnnotations;

namespace DePaso.Api.Models;

public class CalcularRutaModelo
{
    [Required]
    public DireccionViaje Origen { get; set; } = null!;

    [Required]
    public DireccionViaje Destino { get; set; } = null!;

    public List<ParadaViaje> Paradas { get; set; } = new();
}

using System.ComponentModel.DataAnnotations;

namespace DePaso.Api.Models;

public class EnvioAlta
{
    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public DireccionEnvioRegistro Origen { get; set; } = null!;

    [Required]
    public DireccionEnvioRegistro Destino { get; set; } = null!;

    [Required]
    public DateOnly FechaLimite { get; set; }

    [Range(0.01, 9999.99)]
    public decimal PesoKg { get; set; }

    [Range(1, 255)]
    public byte Tamanio { get; set; }

    [Required]
    public string Descripcion { get; set; } = "";

    [Range(0.01, 999999.99)]
    public decimal Monto { get; set; }
}

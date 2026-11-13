using System.ComponentModel.DataAnnotations;

namespace DePaso.Api.Models;

// TODO: ver si se puede eliminar despues de actualizar direcciones
public class EnvioRegistro
{
    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public string OrigenDir { get; set; } = "";

    [Range(-90, 90)]
    public decimal OrigenLat { get; set; }

    [Range(-180, 180)]
    public decimal OrigenLng { get; set; }

    [Required]
    public string DestinoDir { get; set; } = "";

    [Range(-90, 90)]
    public decimal DestinoLat { get; set; }

    [Range(-180, 180)]
    public decimal DestinoLng { get; set; }

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

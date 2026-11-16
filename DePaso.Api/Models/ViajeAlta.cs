using DePaso.Logica.dtos;
using System.ComponentModel.DataAnnotations;

namespace DePaso.Api.Models;

public class ViajeAlta
{
    [Range(1, int.MaxValue, ErrorMessage = "Falta el usuario.")]
    public int UsuarioId { get; set; }

    [Required]
    public DireccionViaje Origen { get; set; } = null!;

    [Required]
    public DireccionViaje Destino { get; set; } = null!;

    [Required, MinLength(1, ErrorMessage = "Elegí al menos un día.")]
    public List<byte> Dias { get; set; } = new();

    [Required]
    public TimeOnly? HoraSalida { get; set; }

    [Required]
    public TimeOnly? HoraLlegada { get; set; }

    [Range(1, 100, ErrorMessage = "La cantidad máxima de paquetes tiene que ser mayor que cero.")]
    public int MaxPaquetes { get; set; }

    [Range(0.01, 999.99, ErrorMessage = "El peso máximo tiene que ser mayor que cero.")]
    public decimal PesoMaxKg { get; set; }

    [Range(0.01, 99.99, ErrorMessage = "El desvío máximo tiene que ser mayor que cero.")]
    public decimal DesvioMaxKm { get; set; }

    public List<ParadaViaje> Paradas { get; set; } = new();

    public ViajeDatos ADatos() =>
        new(
            UsuarioId,
            Origen.ADatos(),
            Destino.ADatos(),
            Dias,
            HoraSalida ?? default,
            HoraLlegada ?? default,
            MaxPaquetes,
            PesoMaxKg,
            DesvioMaxKm,
            (Paradas ?? new()).Select(p => p.AParada()).ToList());
}
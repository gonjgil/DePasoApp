using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DePaso.Entidades;

public partial class Viajes
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    // TODO: eliminar OrigenDir y DestinoDir, despues de adaptar todo
    public string OrigenDir { get; set; } = null!;
    public string DestinoDir { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string OrigenCalle { get; set; } = null!;

    [Required]
    public int OrigenAltura { get; set; }

    [Required]
    [MaxLength(100)]
    public string OrigenLocalidad { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string OrigenProvincia { get; set; } = null!;

    [MaxLength(10)]
    public string? OrigenPiso { get; set; }

    [MaxLength(10)]
    public string? OrigenDepartamento { get; set; }

    [Required]
    public decimal OrigenLat { get; set; }

    [Required]
    public decimal OrigenLng { get; set; }

    [Required]
    [MaxLength(150)]
    public string DestinoCalle { get; set; } = null!;

    [Required]
    public int DestinoAltura { get; set; }

    [Required]
    [MaxLength(100)]
    public string DestinoLocalidad { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string DestinoProvincia { get; set; } = null!;

    [MaxLength(10)]
    public string? DestinoPiso { get; set; }

    [MaxLength(10)]
    public string? DestinoDepartamento { get; set; }

    [Required]
    public decimal DestinoLat { get; set; }

    [Required]
    public decimal DestinoLng { get; set; }
    public TimeOnly HoraSalida { get; set; }

    public TimeOnly HoraLlegada { get; set; }

    public int MaxPaquetes { get; set; }

    public decimal PesoMaxKg { get; set; }

    public decimal DesvioMaxKm { get; set; }

    public decimal DistanciaKm { get; set; }

    public int DuracionMin { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Asignaciones> Asignaciones { get; set; } = new List<Asignaciones>();

    public virtual ICollection<Paradas> Paradas { get; set; } = new List<Paradas>();

    public virtual Usuarios Usuario { get; set; } = null!;

    public virtual ICollection<ViajeDia> ViajeDia { get; set; } = new List<ViajeDia>();
}

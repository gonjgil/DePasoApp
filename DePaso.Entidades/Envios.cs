using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DePaso.Entidades;

public partial class Envios
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

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

    public DateOnly FechaLimite { get; set; }

    public decimal PesoKg { get; set; }

    public byte Tamanio { get; set; }

    public string Descripcion { get; set; } = null!;

    public decimal Monto { get; set; }

    public byte Estado { get; set; }

    public DateTime CreadoEn { get; set; }

    public byte RowVersion { get; set; }

    public virtual Asignaciones? Asignaciones { get; set; }

    public virtual ICollection<EnvioEstadoHistoria> EnvioEstadoHistoria { get; set; } =
        new List<EnvioEstadoHistoria>();

    public virtual Usuarios Usuario { get; set; } = null!;
}

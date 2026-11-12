using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DePaso.Entidades;

public partial class Paradas
{
    public int Id { get; set; }

    public int ViajeId { get; set; }

    public int Orden { get; set; }

    // TODO: eliminar Direccion, despues de adaptar todo
    public string Direccion { get; set; } = null!;

    [Required]
    public string Calle { get; set; } = null!;

    [Required]
    public int Altura { get; set; }

    [Required]
    [MaxLength(100)]
    public string Localidad { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Provincia { get; set; } = null!;

    [MaxLength(10)]
    public string? Piso { get; set; }

    [MaxLength(10)]
    public string? Departamento { get; set; }

    [Required]
    public decimal Lat { get; set; }

    [Required]
    public decimal Lng { get; set; }

    public virtual Viajes Viaje { get; set; } = null!;
}

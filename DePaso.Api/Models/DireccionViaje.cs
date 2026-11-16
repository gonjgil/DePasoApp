using DePaso.Logica.dtos;
using System.ComponentModel.DataAnnotations;

namespace DePaso.Api.Models;

public class DireccionViaje
{
    [Required(ErrorMessage = "Ingresá la calle."), MaxLength(150)]
    public string Calle { get; set; } = "";

    [Range(1, 99999, ErrorMessage = "Ingresá una altura válida.")]
    public int Altura { get; set; }

    [Required(ErrorMessage = "Ingresá la localidad."), MaxLength(100)]
    public string Localidad { get; set; } = "";

    [Required(ErrorMessage = "Ingresá la provincia."), MaxLength(100)]
    public string Provincia { get; set; } = "";

    [MaxLength(10)]
    public string? Piso { get; set; }

    [MaxLength(10)]
    public string? Departamento { get; set; }

    [Range(-90, 90)]
    public decimal? Lat { get; set; }

    [Range(-180, 180)]
    public decimal? Lng { get; set; }

    public DireccionDatos ADatos() => new(Calle, Altura, Piso, Departamento, Localidad, Provincia, Lat, Lng);
}

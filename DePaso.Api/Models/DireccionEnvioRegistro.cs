using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DePaso.Api.Models;

public class DireccionEnvioRegistro
{
    [Required, MaxLength(150)]
    public string Calle { get; set; } = "";

    [Required, Range(1, int.MaxValue)]
    public int Altura { get; set; }

    [Required, MaxLength(100)]
    public string Localidad { get; set; } = "";

    [Required, MaxLength(100)]
    public string Provincia { get; set; } = "";

    [MaxLength(10)]
    public string? Piso { get; set; }

    [MaxLength(10)]
    public string? Departamento { get; set; }
}

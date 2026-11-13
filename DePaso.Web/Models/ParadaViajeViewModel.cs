using System.ComponentModel.DataAnnotations;

namespace DePaso.Web.Models;

public class ParadaViajeViewModel
{
    [MaxLength(150, ErrorMessage = "La calle admite hasta 150 caracteres.")]
    public string? Calle { get; set; }

    public int Altura { get; set; }

    [MaxLength(100, ErrorMessage = "La localidad admite hasta 100 caracteres.")]
    public string? Localidad { get; set; }

    [MaxLength(100, ErrorMessage = "La provincia admite hasta 100 caracteres.")]
    public string? Provincia { get; set; }

    [MaxLength(10, ErrorMessage = "El piso admite hasta 10 caracteres.")]
    public string? Piso { get; set; }

    [MaxLength(10, ErrorMessage = "El departamento admite hasta 10 caracteres.")]
    public string? Departamento { get; set; }

    public decimal Lat { get; set; }

    public decimal Lng { get; set; }

    public bool EstaVacia() =>
        string.IsNullOrWhiteSpace(Calle)
        && Altura <= 0
        && string.IsNullOrWhiteSpace(Localidad)
        && string.IsNullOrWhiteSpace(Provincia)
        && string.IsNullOrWhiteSpace(Piso)
        && string.IsNullOrWhiteSpace(Departamento)
        && Lat == 0
        && Lng == 0;

    public bool EstaCompleta() =>
        !string.IsNullOrWhiteSpace(Calle)
        && Altura > 0
        && !string.IsNullOrWhiteSpace(Localidad)
        && !string.IsNullOrWhiteSpace(Provincia);

    public string Clave() =>
        $"{Calle?.Trim().ToUpperInvariant()}|{Altura}|{Localidad?.Trim().ToUpperInvariant()}";
}

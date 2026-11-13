using System.ComponentModel.DataAnnotations;

namespace DePaso.Web.Models;

public class NuevoViajeViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Ingresá la calle de origen.")]
    [MaxLength(150, ErrorMessage = "La calle admite hasta 150 caracteres.")]
    public string OrigenCalle { get; set; } = "";

    [Required(ErrorMessage = "Ingresá la altura de origen.")]
    [Range(1, 99999, ErrorMessage = "Ingresá la altura de origen.")]
    public int? OrigenAltura { get; set; }

    [Required(ErrorMessage = "Ingresá la localidad de origen.")]
    [MaxLength(100, ErrorMessage = "La localidad admite hasta 100 caracteres.")]
    public string OrigenLocalidad { get; set; } = "";

    [Required(ErrorMessage = "Ingresá la provincia de origen.")]
    [MaxLength(100, ErrorMessage = "La provincia admite hasta 100 caracteres.")]
    public string OrigenProvincia { get; set; } = "";

    [MaxLength(10, ErrorMessage = "El piso admite hasta 10 caracteres.")]
    public string? OrigenPiso { get; set; }

    [MaxLength(10, ErrorMessage = "El departamento admite hasta 10 caracteres.")]
    public string? OrigenDepartamento { get; set; }

    [Range(-90, 90, ErrorMessage = "Ingresá una latitud de origen válida.")]
    public decimal? OrigenLat { get; set; }

    [Range(-180, 180, ErrorMessage = "Ingresá una longitud de origen válida.")]
    public decimal? OrigenLng { get; set; }

    [Required(ErrorMessage = "Ingresá la calle de destino.")]
    [MaxLength(150, ErrorMessage = "La calle admite hasta 150 caracteres.")]
    public string DestinoCalle { get; set; } = "";

    [Required(ErrorMessage = "Ingresá la altura de destino.")]
    [Range(1, 99999, ErrorMessage = "Ingresá la altura de destino.")]
    public int? DestinoAltura { get; set; }

    [Required(ErrorMessage = "Ingresá la localidad de destino.")]
    [MaxLength(100, ErrorMessage = "La localidad admite hasta 100 caracteres.")]
    public string DestinoLocalidad { get; set; } = "";

    [Required(ErrorMessage = "Ingresá la provincia de destino.")]
    [MaxLength(100, ErrorMessage = "La provincia admite hasta 100 caracteres.")]
    public string DestinoProvincia { get; set; } = "";

    [MaxLength(10, ErrorMessage = "El piso admite hasta 10 caracteres.")]
    public string? DestinoPiso { get; set; }

    [MaxLength(10, ErrorMessage = "El departamento admite hasta 10 caracteres.")]
    public string? DestinoDepartamento { get; set; }

    [Range(-90, 90, ErrorMessage = "Ingresá una latitud de destino válida.")]
    public decimal? DestinoLat { get; set; }

    [Range(-180, 180, ErrorMessage = "Ingresá una longitud de destino válida.")]
    public decimal? DestinoLng { get; set; }

    public List<byte> Dias { get; set; } = new();

    [Required(ErrorMessage = "Ingresá la hora de salida.")]
    [DataType(DataType.Time)]
    [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
    public TimeOnly? HoraSalida { get; set; }

    [Required(ErrorMessage = "Ingresá la hora estimada de llegada.")]
    [DataType(DataType.Time)]
    [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
    public TimeOnly? HoraLlegada { get; set; }

    [Required(ErrorMessage = "La cantidad de paquetes tiene que ser mayor que cero.")]
    [Range(1, 100, ErrorMessage = "La cantidad de paquetes tiene que ser mayor que cero.")]
    public int? MaxPaquetes { get; set; }

    [Required(ErrorMessage = "El peso máximo tiene que ser mayor que cero.")]
    [Range(0.01, 9999, ErrorMessage = "El peso máximo tiene que ser mayor que cero.")]
    public decimal? PesoMaxKg { get; set; }

    [Required(ErrorMessage = "El desvío máximo tiene que ser mayor que cero.")]
    [Range(0.01, 999, ErrorMessage = "El desvío máximo tiene que ser mayor que cero.")]
    public decimal? DesvioMaxKm { get; set; }

    public List<ParadaViajeViewModel> Paradas { get; set; } = new();

    public decimal? DistanciaKm { get; set; }

    public int? DuracionMin { get; set; }

    public string ClaveOrigen() =>
        $"{OrigenCalle?.Trim().ToUpperInvariant()}|{OrigenAltura.GetValueOrDefault()}|{OrigenLocalidad?.Trim().ToUpperInvariant()}";

    public string ClaveDestino() =>
        $"{DestinoCalle?.Trim().ToUpperInvariant()}|{DestinoAltura.GetValueOrDefault()}|{DestinoLocalidad?.Trim().ToUpperInvariant()}";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Dias.Count == 0 || Dias.Any(dia => dia is < 1 or > 7))
        {
            yield return new ValidationResult("Elegí al menos un día.", [nameof(Dias)]);
        }

        if (ClaveOrigen() == ClaveDestino())
        {
            yield return new ValidationResult(
                "El origen y el destino tienen que ser distintos.",
                [nameof(DestinoCalle)]
            );
        }

        var vistas = new HashSet<string>();
        for (var i = 0; i < Paradas.Count; i++)
        {
            var parada = Paradas[i];
            if (parada.EstaVacia())
            {
                continue;
            }

            if (!parada.EstaCompleta())
            {
                yield return new ValidationResult(
                    "Completá calle, altura, localidad y provincia de la parada.",
                    [$"Paradas[{i}].Calle"]
                );
                continue;
            }

            if (parada.Lat is < -90 or > 90 || parada.Lng is < -180 or > 180)
            {
                yield return new ValidationResult(
                    "Ingresá coordenadas válidas para la parada.",
                    [$"Paradas[{i}].Lat"]
                );
            }

            var clave = parada.Clave();
            if (clave == ClaveOrigen() || clave == ClaveDestino())
            {
                yield return new ValidationResult(
                    "Una parada no puede coincidir con el origen o el destino.",
                    [$"Paradas[{i}].Calle"]
                );
            }

            if (!vistas.Add(clave))
            {
                yield return new ValidationResult("No repitas paradas.", [$"Paradas[{i}].Calle"]);
            }
        }
    }
}

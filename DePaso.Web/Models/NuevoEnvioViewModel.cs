using System.ComponentModel.DataAnnotations;

namespace DePaso.Web.Models;

public class NuevoEnvioViewModel : IValidatableObject
{
    // TODO: eliminar OrigenDir y DestinoDir, despues de adaptar todo
    public string OrigenDir { get; set; } = "";
    public string DestinoDir { get; set; } = "";

    [Required(ErrorMessage = "Ingresá una dirección de origen.")]
    public string OrigenCalle { get; set; } = "";

    [Required(ErrorMessage = "Ingresá una altura de origen.")]
    [Range(1, 99999, ErrorMessage = "Ingresá una altura válida.")]
    public int OrigenAltura { get; set; }

    public string? OrigenPiso { get; set; }

    public string? OrigenDepartamento { get; set; } = "";

    [Required(ErrorMessage = "Ingresá una localidad de origen.")]
    public string OrigenLocalidad { get; set; } = "";

    [Required(ErrorMessage = "Ingresá una provincia de origen.")]
    public string OrigenProvincia { get; set; } = "";

    [Range(-90, 90, ErrorMessage = "Ingresá una latitud válida.")]
    public decimal OrigenLat { get; set; }

    [Range(-180, 180, ErrorMessage = "Ingresá una longitud válida.")]
    public decimal OrigenLng { get; set; }

    [Required(ErrorMessage = "Ingresá una dirección de destino.")]
    public string DestinoCalle { get; set; } = "";

    [Required(ErrorMessage = "Ingresá una altura de destino.")]
    [Range(1, 99999, ErrorMessage = "Ingresá una altura válida.")]
    public int DestinoAltura { get; set; }

    public string? DestinoPiso { get; set; }

    public string? DestinoDepartamento { get; set; } = "";

    [Required(ErrorMessage = "Ingresá una localidad de destino.")]
    public string DestinoLocalidad { get; set; } = "";

    [Required(ErrorMessage = "Ingresá una provincia de destino.")]
    public string DestinoProvincia { get; set; } = "";

    [Range(-90, 90, ErrorMessage = "Ingresá una latitud válida.")]
    public decimal DestinoLat { get; set; }

    [Range(-180, 180, ErrorMessage = "Ingresá una longitud válida.")]
    public decimal DestinoLng { get; set; }

    [Required(ErrorMessage = "Elegí una fecha límite.")]
    [DataType(DataType.Date)]
    public DateOnly FechaLimite { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Range(0.01, 9999.99, ErrorMessage = "Ingresá un peso válido.")]
    public decimal PesoKg { get; set; }

    [Range(1, 255, ErrorMessage = "Ingresá un tamaño válido.")]
    public byte Tamanio { get; set; } = 1;

    [Required(ErrorMessage = "Describí el envío.")]
    public string Descripcion { get; set; } = "";

    [Range(0.01, 999999.99, ErrorMessage = "Ingresá un monto válido.")]
    public decimal Monto { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
{
    var direccionesCompletas =
        !string.IsNullOrWhiteSpace(OrigenCalle) &&
        OrigenAltura > 0 &&
        !string.IsNullOrWhiteSpace(OrigenLocalidad) &&
        !string.IsNullOrWhiteSpace(OrigenProvincia) &&
        !string.IsNullOrWhiteSpace(DestinoCalle) &&
        DestinoAltura > 0 &&
        !string.IsNullOrWhiteSpace(DestinoLocalidad) &&
        !string.IsNullOrWhiteSpace(DestinoProvincia);

    if (direccionesCompletas)
    {
        var mismoOrigenDestino =
            string.Equals(Normalizar(OrigenCalle), Normalizar(DestinoCalle), StringComparison.OrdinalIgnoreCase) &&
            OrigenAltura == DestinoAltura &&
            string.Equals(Normalizar(OrigenLocalidad), Normalizar(DestinoLocalidad), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Normalizar(OrigenProvincia), Normalizar(DestinoProvincia), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Normalizar(OrigenPiso), Normalizar(DestinoPiso), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Normalizar(OrigenDepartamento), Normalizar(DestinoDepartamento), StringComparison.OrdinalIgnoreCase);

        if (mismoOrigenDestino)
        {
            yield return new ValidationResult(
                "El origen y el destino deben ser diferentes.",
                new[] { nameof(OrigenCalle), nameof(DestinoCalle) });
        }
    }

    if (FechaLimite < DateOnly.FromDateTime(DateTime.Today.AddDays(1)))
    {
        yield return new ValidationResult(
            "La fecha límite debe ser al menos un día después de hoy.",
            new[] { nameof(FechaLimite) });
    }
}

private static string Normalizar(string? valor)
{
    return string.Join(" ", (valor ?? "").Split(
        ' ',
        StringSplitOptions.RemoveEmptyEntries));
}
}

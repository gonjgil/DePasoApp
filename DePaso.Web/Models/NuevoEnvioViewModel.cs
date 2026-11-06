using System.ComponentModel.DataAnnotations;

namespace DePaso.Web.Models;

public class NuevoEnvioViewModel
{
    [Required(ErrorMessage = "Ingresá una dirección de origen.")]
    public string OrigenDir { get; set; } = "";

    [Range(-90, 90, ErrorMessage = "Ingresá una latitud válida.")]
    public decimal OrigenLat { get; set; }

    [Range(-180, 180, ErrorMessage = "Ingresá una longitud válida.")]
    public decimal OrigenLng { get; set; }

    [Required(ErrorMessage = "Ingresá una dirección de destino.")]
    public string DestinoDir { get; set; } = "";

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
}

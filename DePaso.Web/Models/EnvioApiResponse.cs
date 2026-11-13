namespace DePaso.Web.Models;

public class EnvioApiResponse
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string OrigenCalle { get; set; } = "";

    public int OrigenAltura { get; set; }

    public string OrigenPiso { get; set; } = "";

    public string OrigenDepartamento { get; set; } = "";

    public string OrigenLocalidad { get; set; } = "";

    public string OrigenProvincia { get; set; } = "";

    public decimal OrigenLat { get; set; }

    public decimal OrigenLng { get; set; }

    public string DestinoCalle { get; set; } = "";

    public int DestinoAltura { get; set; }

    public string DestinoPiso { get; set; } = "";

    public string DestinoDepartamento { get; set; } = "";

    public string DestinoLocalidad { get; set; } = "";

    public string DestinoProvincia { get; set; } = "";

    public decimal DestinoLat { get; set; }

    public decimal DestinoLng { get; set; }

    public DateOnly FechaLimite { get; set; }

    public decimal PesoKg { get; set; }

    public byte Tamanio { get; set; }

    public string Descripcion { get; set; } = "";

    public decimal Monto { get; set; }

    public byte Estado { get; set; }

    public DateTime CreadoEn { get; set; }
}

namespace DePaso.Web.Models;

public class EnvioApiResponse
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string OrigenDir { get; set; } = "";

    public decimal OrigenLat { get; set; }

    public decimal OrigenLng { get; set; }

    public string DestinoDir { get; set; } = "";

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

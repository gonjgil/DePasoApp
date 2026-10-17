using System;
using System.Collections.Generic;

namespace DePaso.Entidades;

public partial class Asignaciones
{
    public int Id { get; set; }

    public int EnvioId { get; set; }

    public int ViajeId { get; set; }

    public DateOnly FechaViaje { get; set; }

    public decimal DesvioKm { get; set; }

    public int TiempoExtraMin { get; set; }

    public decimal Ganancia { get; set; }

    public DateTime AsignadoEn { get; set; }

    public virtual Envios Envio { get; set; } = null!;

    public virtual Viajes Viaje { get; set; } = null!;
}

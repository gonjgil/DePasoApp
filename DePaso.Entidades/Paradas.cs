using System;
using System.Collections.Generic;

namespace DePaso.Entidades;

public partial class Paradas
{
    public int Id { get; set; }

    public int ViajeId { get; set; }

    public int Orden { get; set; }

    public string Direccion { get; set; } = null!;

    public decimal Lat { get; set; }

    public decimal Lng { get; set; }

    public virtual Viajes Viaje { get; set; } = null!;
}

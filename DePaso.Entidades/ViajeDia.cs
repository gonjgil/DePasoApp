using System;
using System.Collections.Generic;

namespace DePaso.Entidades;

public partial class ViajeDia
{
    public int ViajeId { get; set; }

    public byte DiaSemana { get; set; }

    public virtual Viajes Viaje { get; set; } = null!;
}

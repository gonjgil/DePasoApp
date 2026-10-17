using System;
using System.Collections.Generic;

namespace DePaso.Entidades;

public partial class EnvioEstadoHistoria
{
    public int Id { get; set; }

    public int EnvioId { get; set; }

    public byte EstadoAnterior { get; set; }

    public byte EstadoNuevo { get; set; }

    public DateTime Fecha { get; set; }

    public virtual Envios Envio { get; set; } = null!;
}

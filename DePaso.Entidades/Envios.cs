using System;
using System.Collections.Generic;

namespace DePaso.Entidades;

public partial class Envios
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string OrigenDir { get; set; } = null!;

    public decimal OrigenLat { get; set; }

    public decimal OrigenLng { get; set; }

    public string DestinoDir { get; set; } = null!;

    public decimal DestinoLat { get; set; }

    public decimal DestinoLng { get; set; }

    public DateOnly FechaLimite { get; set; }

    public decimal PesoKg { get; set; }

    public byte Tamanio { get; set; }

    public string Descripcion { get; set; } = null!;

    public decimal Monto { get; set; }

    public byte Estado { get; set; }

    public DateTime CreadoEn { get; set; }

    public byte RowVersion { get; set; }

    public virtual Asignaciones? Asignaciones { get; set; }

    public virtual ICollection<EnvioEstadoHistoria> EnvioEstadoHistoria { get; set; } = new List<EnvioEstadoHistoria>();

    public virtual Usuarios Usuario { get; set; } = null!;
}

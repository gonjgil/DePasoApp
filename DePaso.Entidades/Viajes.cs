using System;
using System.Collections.Generic;

namespace DePaso.Entidades;

public partial class Viajes
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string OrigenDir { get; set; } = null!;

    public decimal OrigenLat { get; set; }

    public decimal OrigenLng { get; set; }

    public string DestinoDir { get; set; } = null!;

    public decimal DestinoLat { get; set; }

    public decimal DestinoLng { get; set; }

    public TimeOnly HoraSalida { get; set; }

    public TimeOnly HoraLlegada { get; set; }

    public int MaxPaquetes { get; set; }

    public decimal PesoMaxKg { get; set; }

    public decimal DesvioMaxKm { get; set; }

    public decimal DistanciaKm { get; set; }

    public int DuracionMin { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Asignaciones> Asignaciones { get; set; } = new List<Asignaciones>();

    public virtual ICollection<Paradas> Paradas { get; set; } = new List<Paradas>();

    public virtual Usuarios Usuario { get; set; } = null!;

    public virtual ICollection<ViajeDia> ViajeDia { get; set; } = new List<ViajeDia>();
}

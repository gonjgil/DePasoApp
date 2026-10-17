using System;
using System.Collections.Generic;

namespace DePaso.Entidades;

public partial class Usuarios
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime CreadoEn { get; set; }

    public virtual ICollection<Envios> Envios { get; set; } = new List<Envios>();

    public virtual ICollection<Viajes> Viajes { get; set; } = new List<Viajes>();
}

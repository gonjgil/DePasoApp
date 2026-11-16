using DePaso.Logica.dtos;
using System.ComponentModel.DataAnnotations;

namespace DePaso.Api.Models;

public class ParadaViaje : DireccionViaje
{
    [Range(1, 100)]
    public int? Orden { get; set; }

    public ParadaDatos AParada() => new(ADatos(), Orden);
}

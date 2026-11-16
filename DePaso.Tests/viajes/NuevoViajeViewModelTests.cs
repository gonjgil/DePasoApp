using System.ComponentModel.DataAnnotations;
using DePaso.Web.Models;

namespace DePaso.Tests.viajes;

public class NuevoViajeViewModelTests
{
    [Fact]
    public void Validate_RecorridoValido_NoTieneErrores()
    {
        var errores = Validar(ModeloValido());

        Assert.Empty(errores);
    }

    [Fact]
    public void Validate_SinDia_PideAlMenosUno()
    {
        var modelo = ModeloValido();
        modelo.Dias.Clear();

        var errores = Validar(modelo);

        Assert.Contains(errores, e => e.ErrorMessage == "Elegí al menos un día.");
    }

    [Fact]
    public void Validate_OrigenIgualDestino_EsError()
    {
        var modelo = ModeloValido();
        modelo.DestinoCalle = " cabildo ";
        modelo.DestinoAltura = 1500;
        modelo.DestinoLocalidad = "BELGRANO";

        var errores = Validar(modelo);

        Assert.Contains(errores, e => e.ErrorMessage == "El origen y el destino tienen que ser distintos.");
    }

    [Fact]
    public void Validate_ParadaIgualAlOrigen_EsError()
    {
        var modelo = ModeloValido();
        modelo.Paradas.Add(Parada("Cabildo", 1500, "Belgrano", "CABA"));

        var errores = Validar(modelo);

        Assert.Contains(errores, e => e.ErrorMessage == "Una parada no puede coincidir con el origen o el destino.");
    }

    [Fact]
    public void Validate_ParadaRepetida_EsError()
    {
        var modelo = ModeloValido();
        modelo.Paradas.Add(Parada("Avellaneda", 100, "Flores", "CABA"));
        modelo.Paradas.Add(Parada("avellaneda", 100, "flores", "CABA"));

        var errores = Validar(modelo);

        Assert.Contains(errores, e => e.ErrorMessage == "No repitas paradas.");
    }

    [Fact]
    public void Validate_ParadaIncompleta_EsError()
    {
        var modelo = ModeloValido();
        modelo.Paradas.Add(new ParadaViajeViewModel { Calle = "Avellaneda" });

        var errores = Validar(modelo);

        Assert.Contains(errores, e => e.ErrorMessage == "Completá calle, altura, localidad y provincia de la parada.");
    }

    private static List<ValidationResult> Validar(NuevoViajeViewModel modelo)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), errores, validateAllProperties: true);
        return errores;
    }

    private static NuevoViajeViewModel ModeloValido() => new()
    {
        OrigenCalle = "Cabildo",
        OrigenAltura = 1500,
        OrigenLocalidad = "Belgrano",
        OrigenProvincia = "CABA",
        DestinoCalle = "Hipólito Yrigoyen",
        DestinoAltura = 800,
        DestinoLocalidad = "Lomas de Zamora",
        DestinoProvincia = "Buenos Aires",
        Dias = [1, 5],
        HoraSalida = new TimeOnly(8, 0),
        HoraLlegada = new TimeOnly(9, 30),
        MaxPaquetes = 3,
        PesoMaxKg = 10,
        DesvioMaxKm = 5
    };

    private static ParadaViajeViewModel Parada(string calle, int altura, string localidad, string provincia) => new()
    {
        Calle = calle,
        Altura = altura,
        Localidad = localidad,
        Provincia = provincia
    };
}

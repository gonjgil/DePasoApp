using DePaso.Logica.helpers;
using static DePaso.Tests.viajes.helper.Datos;
using DePaso.Logica.servicios;

namespace DePaso.Tests.viajes.servicio;

public class ServicioRutasMockTests
{
    private readonly ServicioRutasMock _mock = new();

    [Fact]
    public async Task Geocodificar_MismaDireccion_DevuelveMismasCoordenadas()
    {
        var a = await _mock.GeocodificarAsync(Dir("Av. Hipólito Yrigoyen", 500));
        var b = await _mock.GeocodificarAsync(Dir("av. hipolito  yrigoyen", 500));

        Assert.Equal(a, b);
    }

    [Fact]
    public async Task Geocodificar_DireccionesDistintas_DevuelveCoordenadasDistintas()
    {
        var a = await _mock.GeocodificarAsync(Dir("Av. Cabildo", 1000));
        var b = await _mock.GeocodificarAsync(Dir("Av. Cabildo", 1001));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task Geocodificar_QuedaCercaDeCaba()
    {
        var c = (await _mock.GeocodificarAsync(Dir("Cualquiera", 123)))!.Value;

        Assert.InRange(c.Lat, -34.76, -34.44);
        Assert.InRange(c.Lng, -58.61, -58.29);
    }

    [Fact]
    public async Task CalcularRuta_SumaLosTramosEnOrden()
    {
        var estimador = new EstimadorDesvioHaversine();
        var a = new Coordenada(-34.60, -58.38);
        var b = new Coordenada(-34.58, -58.42);
        var c = new Coordenada(-34.62, -58.45);

        var ruta = await _mock.CalcularRutaAsync(new[] { a, b, c });

        var esperado = estimador.DistanciaKm(a, b) + estimador.DistanciaKm(b, c);
        Assert.Equal(Math.Round((decimal)esperado, 2), ruta.DistanciaKm);
        Assert.True(ruta.Estimada);
    }

    [Fact]
    public async Task CalcularRuta_DuracionA40KmPorHora()
    {
        var ruta = await _mock.CalcularRutaAsync(new[] { new Coordenada(-34.6, -58.4), new Coordenada(-34.7, -58.4) });

        Assert.Equal((int)Math.Round((double)ruta.DistanciaKm / 40 * 60), ruta.DuracionMin, tolerance: 1);
    }

    [Fact]
    public async Task CalcularRuta_ConMenosDeDosPuntos_Lanza()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _mock.CalcularRutaAsync(new[] { new Coordenada(0, 0) }));
    }
}
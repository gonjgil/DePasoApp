using DePaso.Api.Services;
using DePaso.Logica.helpers;
using DePaso.Logica.servicios;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DePaso.Api.Config;

public static class ViajesConfiguracion
{
    public static IServiceCollection AgregarModuloViajes(this IServiceCollection servicio, IConfiguration configuracion)
    {
        servicio.TryAddSingleton<IEstimadorDesvio, EstimadorDesvioHaversine>();
        servicio.TryAddSingleton<IOptimizadorCombinaciones, OptimizadorCombinaciones>();

        var usarMock = configuracion.GetValue<bool>("Rutas:UsarMock")
            || string.IsNullOrWhiteSpace(configuracion["Geoapify:ApiKey"]);

        if (usarMock)
        {
            servicio.AddSingleton<IServicioRutas, ServicioRutasMock>();
        }
        else
        {
            servicio.AddHttpClient<IServicioRutas, ServicioRutasGeoapify>(http =>
            {
                http.BaseAddress = new Uri("https://api.geoapify.com/");
                http.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        servicio.AddScoped<IViajeServicio, ViajeServicio>();
        servicio.AddScoped<IServicioCoincidencias, CoincidenciasServicio>();
        servicio.AddScoped<IOportunidadesServicio, OportunidadesServicio>();
        servicio.AddScoped<ITransportesServicio, TransportesServicio>();
        return servicio;
    }
}

using DePaso.Entidades;
using DePaso.Logica.enums;
using DePaso.Logica.servicios;
using DePaso.Tests.dashboard.helpers;

namespace DePaso.Tests.historial;

public class HistorialServicioTest
{
    [Fact]
    public async Task SinEntregas_DevuelveCeros()
    {
        var ctx = Db.Nuevo();
        Sembrar.Usuario(ctx, 1);
        var servicio = new HistorialServicio(ctx);

        var historial = await servicio.ObtenerAsync(1);

        Assert.NotNull(historial);
        Assert.Equal(0, historial.ViajesRealizados);
        Assert.Equal(0, historial.EnviosTransportados);
        Assert.Equal(0m, historial.GananciaEfectiva);
        Assert.Empty(historial.Operaciones);
    }

    [Fact]
    public async Task UsuarioInexistente_DevuelveNull()
    {
        var servicio = new HistorialServicio(Db.Nuevo());

        Assert.Null(await servicio.ObtenerAsync(99));
    }

    [Fact]
    public async Task AgrupaPorViajeYFecha_YSoloCuentaEntregadosPropios()
    {
        var ctx = Db.Nuevo();
        Sembrar.Usuario(ctx, 1);
        Sembrar.Usuario(ctx, 2);
        Viaje(ctx, 10, 1, "Belgrano", "Lomas de Zamora");
        Viaje(ctx, 20, 2, "Palermo", "San Isidro");

        var septiembre = new DateOnly(2026, 9, 15);
        var agosto = new DateOnly(2026, 8, 3);
        Envio(ctx, 1, "Repuesto", EstadoEnvio.Entregado);
        Envio(ctx, 2, "Caja", EstadoEnvio.Entregado);
        Envio(ctx, 3, "Sobre", EstadoEnvio.Entregado);
        Envio(ctx, 4, "En curso", EstadoEnvio.EnCamino);
        Envio(ctx, 5, "Ajeno", EstadoEnvio.Entregado);
        Asignar(ctx, 1, 10, septiembre, 10000m);
        Asignar(ctx, 2, 10, septiembre, 8000m);
        Asignar(ctx, 3, 10, agosto, 2500m);
        Asignar(ctx, 4, 10, septiembre, 9000m);
        Asignar(ctx, 5, 20, septiembre, 4000m);

        var historial = (await new HistorialServicio(ctx).ObtenerAsync(1))!;

        Assert.Equal(2, historial.ViajesRealizados);
        Assert.Equal(3, historial.EnviosTransportados);
        Assert.Equal(20500m, historial.GananciaEfectiva);
        Assert.Equal("Septiembre · 2 paquetes", historial.Operaciones[0].Detalle);
        Assert.Equal(18000m, historial.Operaciones[0].Ganancia);
        Assert.Equal("Belgrano", historial.Operaciones[0].Origen);
        Assert.Equal("Lomas de Zamora", historial.Operaciones[0].Destino);
        Assert.Equal("Entregado · Sobre", historial.Operaciones[1].Detalle);
        Assert.Equal(2500m, historial.Operaciones[1].Ganancia);
    }

    private static void Viaje(DePasoAppContext ctx, int id, int usuarioId, string origen, string destino)
    {
        ctx.Viajes.Add(new Viajes
        {
            Id = id,
            UsuarioId = usuarioId,
            OrigenCalle = "Calle",
            OrigenAltura = 1,
            OrigenLocalidad = origen,
            OrigenProvincia = "Buenos Aires",
            DestinoCalle = "Calle",
            DestinoAltura = 2,
            DestinoLocalidad = destino,
            DestinoProvincia = "Buenos Aires",
            Activo = true,
        });
        ctx.SaveChanges();
    }

    private static void Envio(DePasoAppContext ctx, int id, string descripcion, EstadoEnvio estado)
    {
        ctx.Envios.Add(new Envios
        {
            Id = id,
            UsuarioId = 9,
            OrigenCalle = "Origen",
            OrigenAltura = 1,
            OrigenLocalidad = "CABA",
            OrigenProvincia = "Buenos Aires",
            DestinoCalle = "Destino",
            DestinoAltura = 2,
            DestinoLocalidad = "CABA",
            DestinoProvincia = "Buenos Aires",
            Descripcion = descripcion,
            Estado = (byte)estado,
            CreadoEn = DateTime.Now,
        });
        ctx.SaveChanges();
    }

    private static void Asignar(DePasoAppContext ctx, int envioId, int viajeId, DateOnly fecha, decimal ganancia)
    {
        ctx.Asignaciones.Add(new Asignaciones
        {
            Id = envioId,
            EnvioId = envioId,
            ViajeId = viajeId,
            FechaViaje = fecha,
            Ganancia = ganancia,
            AsignadoEn = DateTime.Now,
        });
        ctx.SaveChanges();
    }
}

using DePaso.Entidades;
using DePaso.Logica.enums;
using DePaso.Logica.helpers;
using DePaso.Logica.servicios;
using DePaso.Tests.dashboard.helpers;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Tests.coincidencias;

public class AceptarYOportunidadesTests
{
    private static readonly DateOnly Salida = new(2026, 11, 3); // martes
    private static readonly byte[] Martes = { 2 };
    private const int Conductor = 1;
    private const int Otro = 2;

    private static (DePasoAppContext ctx, CoincidenciasServicio svc) Crear()
    {
        var ctx = Db.Nuevo();
        var svc = new CoincidenciasServicio(
            ctx,
            new EstimadorFijo(),
            new OptimizadorCombinaciones(),
            new RutasFijas(),
            new RelojFijo()
        );
        return (ctx, svc);
    }

    [Fact]
    public async Task Aceptar_reserva_los_envios_y_los_pasa_a_conductor_asignado()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes, maxPaquetes: 3, pesoMax: 10, desvioMax: 5);
        Sembrar.Envio(ctx, 10, Otro, 700, pesoKg: 2, desvioKm: 1.5m);
        Sembrar.Envio(ctx, 11, Otro, 300, pesoKg: 1, desvioKm: 0.5m);

        var res = await svc.AceptarAsync(1, Conductor, Salida, new[] { 10, 11 });

        Assert.True(res.Exito);
        Assert.Equal(new[] { 10, 11 }, res.Valor!.Aceptados);
        Assert.Empty(res.Valor.Tomados);
        Assert.Empty(res.Valor.NoEntran);

        var asignacion = ctx.Asignaciones.Single(a => a.EnvioId == 10);
        Assert.Equal(700m, asignacion.Ganancia);
        Assert.Equal(1.5m, asignacion.DesvioKm);
        Assert.Equal(Salida, asignacion.FechaViaje);
        Assert.Equal((byte)EstadoEnvio.ConductorAsignado, ctx.Envios.Single(e => e.Id == 10).Estado);
        Assert.Equal(2, ctx.EnvioEstadoHistoria.Count());
    }

    [Fact]
    public async Task Aceptar_descuenta_la_capacidad_dentro_del_mismo_lote()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes, maxPaquetes: 1);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m);
        Sembrar.Envio(ctx, 11, Otro, 300, desvioKm: 0.5m);

        var res = await svc.AceptarAsync(1, Conductor, Salida, new[] { 10, 11 });

        Assert.Equal(new[] { 10 }, res.Valor!.Aceptados);
        var falla = Assert.Single(res.Valor.NoEntran);
        Assert.Equal(11, falla.EnvioId);
        Assert.Contains("No quedan paquetes", falla.Motivo);
        Assert.Equal((byte)EstadoEnvio.BuscandoConductor, ctx.Envios.Single(e => e.Id == 11).Estado);
    }

    [Fact]
    public async Task Aceptar_avisa_del_envio_que_ya_tomo_otro_conductor_y_reserva_el_resto()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m);
        Sembrar.Envio(ctx, 11, Otro, 300, desvioKm: 0.5m);
        Sembrar.Asignar(ctx, 1, 10, 1, Salida);

        var res = await svc.AceptarAsync(1, Conductor, Salida, new[] { 10, 11 });

        Assert.Equal(new[] { 11 }, res.Valor!.Aceptados);
        Assert.Equal(10, Assert.Single(res.Valor.Tomados).EnvioId);
    }

    [Fact]
    public async Task Aceptar_no_permite_reservar_un_envio_propio()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 11, Conductor, 300, desvioKm: 0.5m);

        var res = await svc.AceptarAsync(1, Conductor, Salida, new[] { 11 });

        Assert.Empty(res.Valor!.Aceptados);
        Assert.Contains("tuyo", Assert.Single(res.Valor.NoEntran).Motivo);
    }

    [Fact]
    public async Task Aceptar_sin_envios_es_invalido()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);

        var res = await svc.AceptarAsync(1, Conductor, Salida, Array.Empty<int>());

        Assert.Equal(TipoError.Validacion, res.Error);
    }

    [Fact]
    public async Task Oportunidades_lista_las_proximas_salidas_con_su_cantidad_de_coincidencias()
    {
        var ctx = Db.Nuevo();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m);
        var coincidencias = new CoincidenciasServicio(
            ctx,
            new EstimadorFijo(),
            new OptimizadorCombinaciones(),
            new RutasFijas(),
            new RelojFijo()
        );
        var svc = new OportunidadesServicio(ctx, coincidencias, new RelojFijo());

        var salidas = await svc.ListarSalidasAsync(Conductor);

        // Martes 27/10 10:00: la salida de hoy (8:00) ya pasó. La ventana de 14 días (27/10 a 9/11) sólo incluye el 3/11.
        Assert.Equal(new[] { new DateOnly(2026, 11, 3) }, salidas.Select(s => s.Fecha));
        Assert.All(salidas, s => Assert.Equal(1, s.Coincidencias));
    }
}

using DePaso.Entidades;
using DePaso.Logica.enums;
using DePaso.Logica.servicios;
using DePaso.Tests.dashboard.helpers;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Tests.transportes;

public class TransportesServicioTests
{
    private static readonly DateOnly Salida = new(2026, 11, 3);
    private static readonly byte[] Martes = { 2 };
    private const int Conductor = 1;
    private const int Otro = 2;

    private static (DePasoAppContext ctx, TransportesServicio svc) Crear()
    {
        var ctx = Db.Nuevo();
        return (ctx, new TransportesServicio(ctx, new RelojFijo()));
    }

    [Fact]
    public void Siguiente_respeta_el_orden_de_estados()
    {
        Assert.Equal(EstadoEnvio.Retirado, TransportesServicio.Siguiente((byte)EstadoEnvio.ConductorAsignado));
        Assert.Equal(EstadoEnvio.EnCamino, TransportesServicio.Siguiente((byte)EstadoEnvio.Retirado));
        Assert.Equal(EstadoEnvio.Entregado, TransportesServicio.Siguiente((byte)EstadoEnvio.EnCamino));
        Assert.Null(TransportesServicio.Siguiente((byte)EstadoEnvio.Entregado));
        Assert.Null(TransportesServicio.Siguiente((byte)EstadoEnvio.BuscandoConductor));
    }

    [Fact]
    public async Task Listar_muestra_las_asignaciones_del_conductor_con_su_siguiente_paso()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m, estado: EstadoEnvio.ConductorAsignado);
        Sembrar.Asignar(ctx, 1, 10, 1, Salida);
        Sembrar.Viaje(ctx, 2, Otro, Martes);
        Sembrar.Envio(ctx, 11, Conductor, 300, desvioKm: 0.5m, estado: EstadoEnvio.ConductorAsignado);
        Sembrar.Asignar(ctx, 2, 11, 2, Salida);

        var lista = await svc.ListarAsync(Conductor);

        var t = Assert.Single(lista);
        Assert.Equal(10, t.EnvioId);
        Assert.Equal((byte)EstadoEnvio.ConductorAsignado, t.Estado);
        Assert.Equal((byte)EstadoEnvio.Retirado, t.Siguiente);
        Assert.Equal("Retirado", t.SiguienteTexto);
    }

    [Fact]
    public async Task Avanzar_pasa_al_siguiente_estado_y_escribe_el_historial()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m, estado: EstadoEnvio.ConductorAsignado);
        var asignacion = Sembrar.Asignar(ctx, 1, 10, 1, Salida);

        var res = await svc.AvanzarAsync(asignacion.Id, Conductor, (byte)EstadoEnvio.ConductorAsignado);

        Assert.True(res.Exito);
        Assert.Equal((byte)EstadoEnvio.Retirado, ctx.Envios.Single(e => e.Id == 10).Estado);
        var historia = Assert.Single(ctx.EnvioEstadoHistoria.Where(h => h.EnvioId == 10));
        Assert.Equal((byte)EstadoEnvio.ConductorAsignado, historia.EstadoAnterior);
        Assert.Equal((byte)EstadoEnvio.Retirado, historia.EstadoNuevo);
    }

    [Fact]
    public async Task Avanzar_con_estado_desactualizado_es_conflicto()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m, estado: EstadoEnvio.Retirado);
        var asignacion = Sembrar.Asignar(ctx, 1, 10, 1, Salida);

        var res = await svc.AvanzarAsync(asignacion.Id, Conductor, (byte)EstadoEnvio.ConductorAsignado);

        Assert.Equal(TipoError.Conflicto, res.Error);
        Assert.Equal((byte)EstadoEnvio.Retirado, ctx.Envios.Single(e => e.Id == 10).Estado);
    }

    [Fact]
    public async Task Avanzar_de_otro_conductor_es_prohibido()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m, estado: EstadoEnvio.ConductorAsignado);
        var asignacion = Sembrar.Asignar(ctx, 1, 10, 1, Salida);

        var res = await svc.AvanzarAsync(asignacion.Id, Otro, (byte)EstadoEnvio.ConductorAsignado);

        Assert.Equal(TipoError.Prohibido, res.Error);
    }

    [Fact]
    public async Task Cancelar_quita_la_asignacion_y_devuelve_el_envio_al_pool()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m, estado: EstadoEnvio.ConductorAsignado);
        var asignacion = Sembrar.Asignar(ctx, 1, 10, 1, Salida);

        var res = await svc.CancelarAsync(asignacion.Id, Conductor, (byte)EstadoEnvio.ConductorAsignado);

        Assert.True(res.Exito);
        Assert.Equal((byte)EstadoEnvio.BuscandoConductor, ctx.Envios.Single(e => e.Id == 10).Estado);
        Assert.Empty(ctx.Asignaciones.Where(a => a.EnvioId == 10));
        var historia = Assert.Single(ctx.EnvioEstadoHistoria.Where(h => h.EnvioId == 10));
        Assert.Equal((byte)EstadoEnvio.ConductorAsignado, historia.EstadoAnterior);
        Assert.Equal((byte)EstadoEnvio.BuscandoConductor, historia.EstadoNuevo);
    }

    [Fact]
    public async Task Cancelar_un_envio_ya_retirado_es_conflicto()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m, estado: EstadoEnvio.Retirado);
        var asignacion = Sembrar.Asignar(ctx, 1, 10, 1, Salida);

        var res = await svc.CancelarAsync(asignacion.Id, Conductor, (byte)EstadoEnvio.Retirado);

        Assert.Equal(TipoError.Conflicto, res.Error);
        Assert.Single(ctx.Asignaciones.Where(a => a.EnvioId == 10));
    }

    [Fact]
    public async Task Cancelar_de_otro_conductor_es_prohibido()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m, estado: EstadoEnvio.ConductorAsignado);
        var asignacion = Sembrar.Asignar(ctx, 1, 10, 1, Salida);

        var res = await svc.CancelarAsync(asignacion.Id, Otro, (byte)EstadoEnvio.ConductorAsignado);

        Assert.Equal(TipoError.Prohibido, res.Error);
        Assert.Single(ctx.Asignaciones.Where(a => a.EnvioId == 10));
    }

    [Fact]
    public async Task Avanzar_un_entregado_es_conflicto()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m, estado: EstadoEnvio.Entregado);
        var asignacion = Sembrar.Asignar(ctx, 1, 10, 1, Salida);

        var res = await svc.AvanzarAsync(asignacion.Id, Conductor, (byte)EstadoEnvio.Entregado);

        Assert.Equal(TipoError.Conflicto, res.Error);
    }
}

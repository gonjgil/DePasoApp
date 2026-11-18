using DePaso.Entidades;
using DePaso.Logica.dtos;
using DePaso.Logica.enums;
using DePaso.Logica.helpers;
using DePaso.Logica.servicios;
using DePaso.Tests.dashboard.helpers;

namespace DePaso.Tests.coincidencias;

public class CoincidenciasServicioTests
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
    public async Task Listar_excluye_propios_asignados_no_buscando_y_vencidos()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 500, desvioKm: 0.5m);
        Sembrar.Envio(ctx, 11, Conductor, 500, desvioKm: 0.5m);
        Sembrar.Envio(ctx, 12, Otro, 500, desvioKm: 0.5m);
        Sembrar.Asignar(ctx, 1, 12, 1, new DateOnly(2026, 11, 10));
        Sembrar.Envio(ctx, 13, Otro, 500, desvioKm: 0.5m, fechaLimite: new DateOnly(2026, 11, 1));
        Sembrar.Envio(ctx, 14, Otro, 500, desvioKm: 0.5m, estado: EstadoEnvio.ConductorAsignado);

        var res = await svc.ListarAsync(1, Conductor, Salida);

        Assert.True(res.Exito);
        Assert.Equal(new[] { 10 }, res.Valor!.Coincidencias.Select(c => c.EnvioId));
    }

    [Fact]
    public async Task Listar_clasifica_por_nivel_de_desvio()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes, desvioMax: 5);
        Sembrar.Envio(ctx, 10, Otro, 100, desvioKm: 0.5m);
        Sembrar.Envio(ctx, 11, Otro, 100, desvioKm: 1.5m);
        Sembrar.Envio(ctx, 12, Otro, 100, desvioKm: 3m);
        Sembrar.Envio(ctx, 13, Otro, 100, desvioKm: 1m);

        var res = await svc.ListarAsync(1, Conductor, Salida);

        var niveles = res.Valor!.Coincidencias.ToDictionary(c => c.EnvioId, c => c.Nivel);
        Assert.Equal(NivelCoincidencia.MuyCompatible, niveles[10]);
        Assert.Equal(NivelCoincidencia.Compatible, niveles[11]);
        Assert.Equal(NivelCoincidencia.MuchoDesvio, niveles[12]);
        Assert.Equal(NivelCoincidencia.MuyCompatible, niveles[13]);
    }

    [Fact]
    public async Task Listar_resta_lo_asignado_a_esa_salida()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes, maxPaquetes: 2, pesoMax: 10, desvioMax: 5);
        Sembrar.Envio(ctx, 20, Otro, 100, pesoKg: 8, desvioKm: 1);
        Sembrar.Asignar(ctx, 1, 20, 1, Salida, desvioKm: 1);
        Sembrar.Envio(ctx, 10, Otro, 100, pesoKg: 3, desvioKm: 0.5m);
        Sembrar.Envio(ctx, 11, Otro, 100, pesoKg: 1, desvioKm: 0.5m);

        var res = await svc.ListarAsync(1, Conductor, Salida);

        Assert.Equal(new[] { 11 }, res.Valor!.Coincidencias.Select(c => c.EnvioId));
        Assert.Equal(1, res.Valor.Capacidad.Paquetes);
        Assert.Equal(2m, res.Valor.Capacidad.PesoKg);
        Assert.Equal(4m, res.Valor.Capacidad.DesvioKm);
    }

    [Fact]
    public async Task Listar_no_resta_asignaciones_de_otra_fecha()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes, maxPaquetes: 2);
        Sembrar.Envio(ctx, 20, Otro, 100, pesoKg: 1, desvioKm: 1);
        Sembrar.Asignar(ctx, 1, 20, 1, new DateOnly(2026, 11, 10), desvioKm: 1);

        var res = await svc.ListarAsync(1, Conductor, Salida);

        Assert.Equal(2, res.Valor!.Capacidad.Paquetes);
    }

    [Fact]
    public async Task Detalle_envio_que_entra_puede_aceptarse_y_muestra_rutas()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes, maxPaquetes: 3, pesoMax: 10, desvioMax: 5);
        Sembrar.Envio(ctx, 10, Otro, 700, pesoKg: 2, desvioKm: 1.5m);

        var res = await svc.DetalleAsync(1, 10, Conductor, Salida);

        Assert.True(res.Exito);
        var d = res.Valor!;
        Assert.True(d.PuedeAceptarse);
        Assert.Empty(d.Motivos);
        Assert.Equal(2m, d.RutaHabitual.Km);
        Assert.Equal(20, d.RutaHabitual.Minutos);
        Assert.Equal(4m, d.RutaConEnvio.Km);
        Assert.Equal(40, d.RutaConEnvio.Minutos);
        Assert.Equal(1.5m, d.DesvioKm);
        Assert.Equal(700m, d.Pago);
        Assert.Equal(2, d.CapacidadQueda.Paquetes);
        Assert.Equal(8m, d.CapacidadQueda.PesoKg);
        Assert.Equal(3.5m, d.CapacidadQueda.DesvioKm);
    }

    [Fact]
    public async Task Detalle_envio_que_supera_peso_y_desvio_no_puede_aceptarse()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes, pesoMax: 10, desvioMax: 5);
        Sembrar.Envio(ctx, 10, Otro, 700, pesoKg: 12, desvioKm: 6);

        var res = await svc.DetalleAsync(1, 10, Conductor, Salida);

        Assert.False(res.Valor!.PuedeAceptarse);
        Assert.Contains(res.Valor.Motivos, m => m.Contains("peso"));
        Assert.Contains(res.Valor.Motivos, m => m.Contains("desvío"));
    }

    [Fact]
    public async Task Detalle_envio_ya_asignado_no_puede_aceptarse()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 10, Otro, 700, desvioKm: 0.5m);
        Sembrar.Asignar(ctx, 1, 10, 1, new DateOnly(2026, 11, 10));

        var res = await svc.DetalleAsync(1, 10, Conductor, Salida);

        Assert.False(res.Valor!.PuedeAceptarse);
        Assert.Contains(res.Valor.Motivos, m => m.Contains("ya tiene conductor asignado"));
    }

    [Fact]
    public async Task Detalle_de_envio_propio_no_encontrado()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);
        Sembrar.Envio(ctx, 11, Conductor, 700, desvioKm: 0.5m);

        var res = await svc.DetalleAsync(1, 11, Conductor, Salida);

        Assert.Equal(TipoError.NoEncontrado, res.Error);
    }

    [Fact]
    public async Task Optimizar_elige_la_combinacion_de_mayor_pago_que_no_supera_topes()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes, maxPaquetes: 2, pesoMax: 10, desvioMax: 5);
        Sembrar.Envio(ctx, 10, Otro, 500, pesoKg: 4, desvioKm: 1);
        Sembrar.Envio(ctx, 11, Otro, 300, pesoKg: 3, desvioKm: 0.5m);
        Sembrar.Envio(ctx, 12, Otro, 400, pesoKg: 6, desvioKm: 0.2m);

        var res = await svc.OptimizarAsync(1, Conductor, Salida);

        var r = res.Valor!;
        Assert.Equal(new[] { 10, 12 }, r.Recomendada.EnvioIds.OrderBy(i => i));
        Assert.Equal(900m, r.Recomendada.Pago);
        Assert.Equal(EstadoOpcionCoincidencia.Alternativo, r.Opciones.Single(o => o.EnvioId == 11).Estado);
    }

    [Fact]
    public async Task Optimizar_ante_empate_de_pago_gana_el_de_menor_desvio()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes, maxPaquetes: 1);
        Sembrar.Envio(ctx, 10, Otro, 500, desvioKm: 2);
        Sembrar.Envio(ctx, 11, Otro, 500, desvioKm: 1);

        var res = await svc.OptimizarAsync(1, Conductor, Salida);

        Assert.Equal(new[] { 11 }, res.Valor!.Recomendada.EnvioIds);
    }

    [Fact]
    public async Task Optimizar_informa_cuando_se_alcanza_el_tope_de_candidatos()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes, maxPaquetes: 2, pesoMax: 10, desvioMax: 5);
        for (var i = 1; i <= 26; i++)
        {
            Sembrar.Envio(ctx, 9 + i, Otro, 100, pesoKg: 0.1m, desvioKm: i / 10m);
        }

        var res = await svc.OptimizarAsync(1, Conductor, Salida);

        var r = res.Valor!;
        Assert.True(r.TopeAlcanzado);
        Assert.Equal(OptimizadorCombinaciones.MAX_CANDIDATOS, r.TopeCandidatos);
        var fueraDeTope = r.Opciones.Single(o => o.EnvioId == 35);
        Assert.Equal(EstadoOpcionCoincidencia.NoEntra, fueraDeTope.Estado);
        Assert.Contains(fueraDeTope.Motivos, m => m.Contains("Fuera de los"));
    }

    [Fact]
    public async Task Salida_de_viaje_ajeno_es_prohibida()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);

        var res = await svc.ListarAsync(1, Otro, Salida);

        Assert.Equal(TipoError.Prohibido, res.Error);
    }

    [Fact]
    public async Task Salida_en_un_dia_que_el_viaje_no_hace_es_invalida()
    {
        var (ctx, svc) = Crear();
        Sembrar.Viaje(ctx, 1, Conductor, Martes);

        var res = await svc.ListarAsync(1, Conductor, new DateOnly(2026, 11, 4)); // miércoles

        Assert.Equal(TipoError.Validacion, res.Error);
    }

    [Fact]
    public void RutaConEnvio_inserta_retiro_y_entrega_en_el_tramo_de_menor_desvio()
    {
        var ruta = new List<Coordenada>
        {
            new(0, 0),
            new(0, 1),
            new(0, 2),
        };
        var origen = new Coordenada(0, 0.5);
        var destino = new Coordenada(0, 0.7);

        // EstimadorFijo.DistanciaKm es 0 para todo: el primer tramo gana por orden.
        var resultado = ReglasCoincidencia.RutaConEnvio(new EstimadorFijo(), ruta, origen, destino);

        Assert.Equal(5, resultado.Count);
        Assert.Equal(origen, resultado[1]);
        Assert.Equal(destino, resultado[2]);
    }
}

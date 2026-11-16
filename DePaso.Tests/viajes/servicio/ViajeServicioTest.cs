using DePaso.Logica.dtos;
using DePaso.Logica.enums;
using DePaso.Logica.servicios;
using DePaso.Tests.viajes.helper;
using static DePaso.Tests.viajes.helper.Datos;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Tests.viajes.servicio;

public class ViajeServicioTests
{
    private readonly EscenarioViajes _esc = new();

    public ViajeServicioTests()
    {
        _esc.Usuario(1);
        _esc.Usuario(2);
    }

    [Fact]
    public async Task Crear_ConDatosValidos_GuardaViajeActivoConDiasParadasYRuta()
    {
        var datos = Viaje(paradas: new[] { Parada("Av. Santa Fe"), Parada("Av. Belgrano") });

        var r = await _esc.Servicio().CrearAsync(datos);

        Assert.True(r.Exito, r.Mensaje);
        var v = r.Valor!;
        Assert.True(v.Activo);
        Assert.Equal(new List<byte> { 1, 3, 5 }, v.Dias);
        Assert.Equal("Lun, Mié, Vie", v.DiasTexto);
        Assert.Equal(new[] { "Av. Santa Fe", "Av. Belgrano" }, v.Paradas.Select(p => p.Direccion.Calle));
        Assert.Equal(new[] { 1, 2 }, v.Paradas.Select(p => p.Orden));
        Assert.Equal(30m, v.DistanciaKm);
        Assert.Equal(45, v.DuracionMin);

        using var ctx = _esc.Contexto();
        var guardado = await ctx.Viajes.Include(x => x.ViajeDia).Include(x => x.Paradas).SingleAsync();
        Assert.Equal(1, guardado.UsuarioId);
        Assert.True(guardado.Activo);
        Assert.Equal(3, guardado.ViajeDia.Count);
        Assert.Equal(2, guardado.Paradas.Count);
        Assert.All(guardado.Paradas, p => Assert.False(string.IsNullOrWhiteSpace(p.Direccion)));
    }

    [Fact]
    public async Task Crear_RutaPasaPorOrigenParadasYDestinoEnOrden()
    {
        var origen = Dir("Origen", lat: -34.1m, lng: -58.1m);
        var destino = Dir("Destino", lat: -34.3m, lng: -58.3m);
        var parada = new ParadaDatos(Dir("Parada", lat: -34.2m, lng: -58.2m));

        await _esc.Servicio().CrearAsync(Viaje(origen: origen, destino: destino, paradas: new[] { parada }));

        var ruta = Assert.Single(_esc.Rutas.Rutas);
        Assert.Equal(new[] { -34.1, -34.2, -34.3 }, ruta.Select(c => c.Lat));
    }

    [Fact]
    public async Task Crear_ConCoordenadas_NoGeocodifica()
    {
        var datos = Viaje(origen: Dir("A", lat: -34.5m, lng: -58.4m), destino: Dir("B", lat: -34.7m, lng: -58.4m));

        var r = await _esc.Servicio().CrearAsync(datos);

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(0, _esc.Rutas.LlamadasGeocodificar);
        Assert.Equal(-34.5m, r.Valor!.Origen.Lat);
    }

    [Fact]
    public async Task Crear_SinCoordenadas_UsaElGeocodificador()
    {
        var r = await _esc.Servicio().CrearAsync(Viaje());

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(2, _esc.Rutas.LlamadasGeocodificar);
        Assert.NotEqual(0m, r.Valor!.Origen.Lat);
    }

    [Fact]
    public async Task Crear_UsuarioInexistente_DevuelveNoEncontrado()
    {
        var r = await _esc.Servicio().CrearAsync(Viaje(usuarioId: 404));

        Assert.Equal(TipoError.NoEncontrado, r.Error);
    }

    [Fact]
    public async Task Crear_OrigenIgualDestino_IgnorandoMayusculasYTildes_EsInvalido()
    {
        var r = await _esc.Servicio().CrearAsync(
            Viaje(origen: Dir("Av. Hipólito Yrigoyen", 500), destino: Dir("AV. HIPOLITO  YRIGOYEN", 500)));

        Assert.Equal(TipoError.Validacion, r.Error);
        Assert.Contains("El origen y el destino tienen que ser distintos.", r.Errores);
    }

    [Fact]
    public async Task Crear_OrigenYDestinoConMismasCoordenadas_EsInvalido()
    {
        var r = await _esc.Servicio().CrearAsync(
            Viaje(origen: Dir("A", lat: -34.5m, lng: -58.4m), destino: Dir("B", lat: -34.5m, lng: -58.4m)));

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Fact]
    public async Task Crear_SinDias_EsInvalido()
    {
        var r = await _esc.Servicio().CrearAsync(Viaje(dias: Array.Empty<byte>()));

        Assert.Equal(TipoError.Validacion, r.Error);
        Assert.Contains("Elegí al menos un día.", r.Errores);
    }

    [Fact]
    public async Task Crear_DiaFueraDeRango_EsInvalido()
    {
        var r = await _esc.Servicio().CrearAsync(Viaje(dias: new byte[] { 0, 8 }));

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Fact]
    public async Task Crear_DiasRepetidos_SeGuardanUnaVez()
    {
        var r = await _esc.Servicio().CrearAsync(Viaje(dias: new byte[] { 5, 1, 5 }));

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(new List<byte> { 1, 5 }, r.Valor!.Dias);
    }

    [Theory]
    [InlineData(8, 0, 8, 0)]
    [InlineData(9, 0, 8, 30)]
    public async Task Crear_LlegadaNoPosteriorALaSalida_EsInvalido(int hs, int ms, int hl, int ml)
    {
        var r = await _esc.Servicio().CrearAsync(Viaje(salida: new TimeOnly(hs, ms), llegada: new TimeOnly(hl, ml)));

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Theory]
    [InlineData(0, 10, 5)]
    [InlineData(3, 0, 5)]
    [InlineData(3, 10, 0)]
    [InlineData(-1, 10, 5)]
    [InlineData(3, -2, 5)]
    public async Task Crear_CapacidadPesoODesvioNoPositivos_EsInvalido(int max, int peso, int desvio)
    {
        var r = await _esc.Servicio().CrearAsync(Viaje(maxPaquetes: max, pesoMax: peso, desvioMax: desvio));

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Fact]
    public async Task Crear_ParadaRepetida_EsInvalido()
    {
        var r = await _esc.Servicio().CrearAsync(Viaje(paradas: new[] { Parada("Av. Santa Fe"), Parada("av. santa fe") }));

        Assert.Equal(TipoError.Validacion, r.Error);
        Assert.Contains("La parada 2 está repetida.", r.Errores);
    }

    [Fact]
    public async Task Crear_ParadaIgualAlOrigenOAlDestino_EsInvalido()
    {
        var r = await _esc.Servicio().CrearAsync(
            Viaje(paradas: new[] { new ParadaDatos(Dir("Av. Cabildo", 1000)) }));

        Assert.Equal(TipoError.Validacion, r.Error);
        Assert.Contains("La parada 1 no puede coincidir con el origen o el destino.", r.Errores);
    }

    [Fact]
    public async Task Crear_ParadaIncompleta_EsInvalido()
    {
        var r = await _esc.Servicio().CrearAsync(Viaje(paradas: new[] { new ParadaDatos(Dir("", 0)) }));

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Fact]
    public async Task Crear_ParadasConOrden_SeGuardanSegunElOrden()
    {
        var r = await _esc.Servicio().CrearAsync(
            Viaje(paradas: new[] { Parada("Tercera", 3), Parada("Primera", 1), Parada("Segunda", 2) }));

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(new[] { "Primera", "Segunda", "Tercera" }, r.Valor!.Paradas.Select(p => p.Direccion.Calle));
        Assert.Equal(new[] { 1, 2, 3 }, r.Valor.Paradas.Select(p => p.Orden));
    }

    [Fact]
    public async Task Crear_OrdenDeParadaRepetido_EsInvalido()
    {
        var r = await _esc.Servicio().CrearAsync(Viaje(paradas: new[] { Parada("Una", 1), Parada("Otra", 1) }));

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Fact]
    public async Task Crear_DireccionQueNoSePuedeUbicar_EsInvalido()
    {
        _esc.Rutas.CallesNoUbicables.Add("Calle Falsa");

        var r = await _esc.Servicio().CrearAsync(Viaje(destino: Dir("Calle Falsa", 123)));

        Assert.Equal(TipoError.Validacion, r.Error);
        Assert.Equal("No se pudo ubicar la dirección de destino.", r.Mensaje);
    }

    [Fact]
    public async Task Crear_RecorridoMasLargoQueElMaximoGuardable_EsInvalidoYNoGuarda()
    {
        _esc.Rutas.DistanciaFija = 150m;

        var r = await _esc.Servicio().CrearAsync(Viaje());

        Assert.Equal(TipoError.Validacion, r.Error);
        using var ctx = _esc.Contexto();
        Assert.Empty(ctx.Viajes);
    }

    [Fact]
    public async Task Listar_SoloDevuelveViajesDelUsuario()
    {
        await _esc.ViajeCreado(Viaje(usuarioId: 1));
        await _esc.ViajeCreado(Viaje(usuarioId: 1, salida: new TimeOnly(18, 0), llegada: new TimeOnly(19, 0)));
        await _esc.ViajeCreado(Viaje(usuarioId: 2));

        var lista = await _esc.Servicio().ListarAsync(1, FiltroViajes.Todos);

        Assert.Equal(2, lista.Count);
    }

    [Theory]
    [InlineData(FiltroViajes.Todos, 2)]
    [InlineData(FiltroViajes.Activos, 1)]
    [InlineData(FiltroViajes.Inactivos, 1)]
    public async Task Listar_FiltraPorEstado(FiltroViajes filtro, int esperados)
    {
        await _esc.ViajeCreado(Viaje());
        var inactivo = await _esc.ViajeCreado(Viaje(salida: new TimeOnly(18, 0), llegada: new TimeOnly(19, 0)));
        await _esc.Servicio().DesactivarAsync(inactivo.Id, 1);

        var lista = await _esc.Servicio().ListarAsync(1, filtro);

        Assert.Equal(esperados, lista.Count);
    }

    [Fact]
    public async Task Listar_CuentaSoloPaquetesEnCurso()
    {
        var v = await _esc.ViajeCreado(Viaje());
        _esc.EnvioAsignado(10, v.Id, EstadoEnvio.ConductorAsignado);
        _esc.EnvioAsignado(11, v.Id, EstadoEnvio.Entregado);

        var resumen = Assert.Single(await _esc.Servicio().ListarAsync(1, FiltroViajes.Todos));

        Assert.Equal(1, resumen.PaquetesEnCurso);
    }

    [Fact]
    public async Task Obtener_Propio_IncluyeParadasYPaquetesAsignados()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("Av. Santa Fe") }));
        _esc.EnvioAsignado(10, v.Id, EstadoEnvio.Retirado, pesoKg: 3);

        var r = await _esc.Servicio().ObtenerAsync(v.Id, 1);

        Assert.True(r.Exito);
        Assert.Single(r.Valor!.Paradas);
        var paquete = Assert.Single(r.Valor.PaquetesAsignados);
        Assert.Equal(10, paquete.EnvioId);
        Assert.Equal(EstadoEnvio.Retirado, paquete.Estado);
        Assert.Equal("Retirado", paquete.EstadoTexto);
    }

    [Fact]
    public async Task Obtener_DeOtroUsuario_EsProhibido()
    {
        var v = await _esc.ViajeCreado(Viaje(usuarioId: 1));

        var r = await _esc.Servicio().ObtenerAsync(v.Id, 2);

        Assert.Equal(TipoError.Prohibido, r.Error);
    }

    [Fact]
    public async Task Obtener_Inexistente_EsNoEncontrado()
    {
        var r = await _esc.Servicio().ObtenerAsync(999, 1);

        Assert.Equal(TipoError.NoEncontrado, r.Error);
    }

    [Fact]
    public async Task Actualizar_Propio_ModificaHorariosCapacidadYDias()
    {
        var v = await _esc.ViajeCreado(Viaje());

        var r = await _esc.Servicio().ActualizarAsync(
            v.Id,
            Viaje(dias: new byte[] { 2, 4 }, salida: new TimeOnly(7, 0), llegada: new TimeOnly(8, 0), maxPaquetes: 5, pesoMax: 20));

        Assert.True(r.Exito, r.Mensaje);
        using var ctx = _esc.Contexto();
        var g = await ctx.Viajes.Include(x => x.ViajeDia).SingleAsync();
        Assert.Equal(new byte[] { 2, 4 }, g.ViajeDia.Select(d => d.DiaSemana).OrderBy(d => d));
        Assert.Equal(new TimeOnly(7, 0), g.HoraSalida);
        Assert.Equal(5, g.MaxPaquetes);
        Assert.Equal(20m, g.PesoMaxKg);
    }

    [Fact]
    public async Task Actualizar_SinCambiarElRecorrido_NoRecalculaLaRuta()
    {
        var v = await _esc.ViajeCreado(Viaje());
        var llamadas = _esc.Rutas.LlamadasRuta;

        var r = await _esc.Servicio().ActualizarAsync(v.Id, Viaje(maxPaquetes: 4));

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(llamadas, _esc.Rutas.LlamadasRuta);
    }

    [Fact]
    public async Task Actualizar_CambiandoElDestino_RecalculaRutaYConservaParadasExistentes()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("Av. Santa Fe") }));
        var idParada = v.Paradas[0].Id;

        var r = await _esc.Servicio().ActualizarAsync(
            v.Id,
            Viaje(destino: Dir("Av. Mitre", 300, "Avellaneda"), paradas: new[] { Parada("Av. Santa Fe"), Parada("Av. Corrientes") }));

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal("Av. Mitre", r.Valor!.Destino.Calle);
        Assert.Equal(idParada, r.Valor.Paradas[0].Id);
        Assert.Equal(2, r.Valor.Paradas.Count);
        Assert.Equal(30m, r.Valor.DistanciaKm);
    }

    [Fact]
    public async Task Actualizar_QuitandoParadas_LasBorraDeLaBase()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("Av. Santa Fe"), Parada("Av. Corrientes") }));

        var r = await _esc.Servicio().ActualizarAsync(v.Id, Viaje());

        Assert.True(r.Exito, r.Mensaje);
        using var ctx = _esc.Contexto();
        Assert.Empty(ctx.Paradas);
    }

    [Fact]
    public async Task Actualizar_DeOtroUsuario_EsProhibido()
    {
        var v = await _esc.ViajeCreado(Viaje(usuarioId: 1));

        var r = await _esc.Servicio().ActualizarAsync(v.Id, Viaje(usuarioId: 2));

        Assert.Equal(TipoError.Prohibido, r.Error);
    }

    [Fact]
    public async Task Actualizar_Inexistente_EsNoEncontrado()
    {
        var r = await _esc.Servicio().ActualizarAsync(999, Viaje());

        Assert.Equal(TipoError.NoEncontrado, r.Error);
    }

    [Fact]
    public async Task Actualizar_DatosInvalidos_EsInvalido()
    {
        var v = await _esc.ViajeCreado(Viaje());

        var r = await _esc.Servicio().ActualizarAsync(v.Id, Viaje(maxPaquetes: 0));

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Fact]
    public async Task Actualizar_ConEnviosEnCurso_NoPermiteCambiarRecorrido()
    {
        var v = await _esc.ViajeCreado(Viaje());
        _esc.EnvioAsignado(10, v.Id, EstadoEnvio.ConductorAsignado);

        var r = await _esc.Servicio().ActualizarAsync(v.Id, Viaje(destino: Dir("Otra calle", 10)));

        Assert.Equal(TipoError.Conflicto, r.Error);
    }

    [Fact]
    public async Task Actualizar_ConEnviosEnCurso_NoPermiteCambiarDias()
    {
        var v = await _esc.ViajeCreado(Viaje());
        _esc.EnvioAsignado(10, v.Id, EstadoEnvio.EnCamino);

        var r = await _esc.Servicio().ActualizarAsync(v.Id, Viaje(dias: new byte[] { 6 }));

        Assert.Equal(TipoError.Conflicto, r.Error);
    }

    [Fact]
    public async Task Actualizar_ConEnviosEnCurso_NoPermiteCapacidadMenorALoAsignado()
    {
        var v = await _esc.ViajeCreado(Viaje(maxPaquetes: 3));
        var fecha = new DateOnly(2026, 11, 20);
        _esc.EnvioAsignado(10, v.Id, EstadoEnvio.ConductorAsignado, fecha: fecha);
        _esc.EnvioAsignado(11, v.Id, EstadoEnvio.ConductorAsignado, fecha: fecha);

        var r = await _esc.Servicio().ActualizarAsync(v.Id, Viaje(maxPaquetes: 1));

        Assert.Equal(TipoError.Conflicto, r.Error);
    }

    [Fact]
    public async Task Actualizar_ConEnviosEnCurso_NoPermitePesoMenorALoAsignado()
    {
        var v = await _esc.ViajeCreado(Viaje(pesoMax: 10));
        _esc.EnvioAsignado(10, v.Id, EstadoEnvio.ConductorAsignado, pesoKg: 6);

        var r = await _esc.Servicio().ActualizarAsync(v.Id, Viaje(pesoMax: 5));

        Assert.Equal(TipoError.Conflicto, r.Error);
    }

    [Fact]
    public async Task Actualizar_ConEnviosEnCurso_PermiteCambiarHorario()
    {
        var v = await _esc.ViajeCreado(Viaje());
        _esc.EnvioAsignado(10, v.Id, EstadoEnvio.ConductorAsignado);

        var r = await _esc.Servicio().ActualizarAsync(v.Id, Viaje(salida: new TimeOnly(9, 0), llegada: new TimeOnly(10, 0)));

        Assert.True(r.Exito, r.Mensaje);
    }

    [Fact]
    public async Task Actualizar_ConEnviosEntregados_PermiteCambiarRecorrido()
    {
        var v = await _esc.ViajeCreado(Viaje());
        _esc.EnvioAsignado(10, v.Id, EstadoEnvio.Entregado);

        var r = await _esc.Servicio().ActualizarAsync(v.Id, Viaje(destino: Dir("Otra calle", 10)));

        Assert.True(r.Exito, r.Mensaje);
    }

    [Fact]
    public async Task Desactivar_ViajeActivo_QuedaInactivo()
    {
        var v = await _esc.ViajeCreado(Viaje());

        var r = await _esc.Servicio().DesactivarAsync(v.Id, 1);

        Assert.True(r.Exito);
        Assert.False(r.Valor!.Activo);
        using var ctx = _esc.Contexto();
        Assert.False((await ctx.Viajes.SingleAsync()).Activo);
    }

    [Fact]
    public async Task Desactivar_YaInactivo_EsConflicto()
    {
        var v = await _esc.ViajeCreado(Viaje());
        await _esc.Servicio().DesactivarAsync(v.Id, 1);

        var r = await _esc.Servicio().DesactivarAsync(v.Id, 1);

        Assert.Equal(TipoError.Conflicto, r.Error);
    }

    [Fact]
    public async Task Desactivar_ConEnviosEnCurso_EsConflicto()
    {
        var v = await _esc.ViajeCreado(Viaje());
        _esc.EnvioAsignado(10, v.Id, EstadoEnvio.Retirado);

        var r = await _esc.Servicio().DesactivarAsync(v.Id, 1);

        Assert.Equal(TipoError.Conflicto, r.Error);
    }

    [Fact]
    public async Task Desactivar_DeOtroUsuario_EsProhibido()
    {
        var v = await _esc.ViajeCreado(Viaje(usuarioId: 1));

        var r = await _esc.Servicio().DesactivarAsync(v.Id, 2);

        Assert.Equal(TipoError.Prohibido, r.Error);
    }

    [Fact]
    public async Task Activar_ViajeInactivo_QuedaActivo()
    {
        var v = await _esc.ViajeCreado(Viaje());
        await _esc.Servicio().DesactivarAsync(v.Id, 1);

        var r = await _esc.Servicio().ActivarAsync(v.Id, 1);

        Assert.True(r.Exito);
        Assert.True(r.Valor!.Activo);
    }

    [Fact]
    public async Task Activar_YaActivo_EsConflicto()
    {
        var v = await _esc.ViajeCreado(Viaje());

        var r = await _esc.Servicio().ActivarAsync(v.Id, 1);

        Assert.Equal(TipoError.Conflicto, r.Error);
    }

    [Fact]
    public async Task AgregarParada_SinOrden_QuedaAlFinalYRecalculaRuta()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("Primera") }));

        var r = await _esc.Servicio().AgregarParadaAsync(v.Id, 1, Parada("Segunda"));

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(new[] { "Primera", "Segunda" }, r.Valor!.Paradas.Select(p => p.Direccion.Calle));
        Assert.Equal(30m, r.Valor.DistanciaKm);
    }

    [Fact]
    public async Task AgregarParada_ConOrden_SeInsertaEnEsaPosicion()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("A"), Parada("C") }));

        var r = await _esc.Servicio().AgregarParadaAsync(v.Id, 1, Parada("B", orden: 2));

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(new[] { "A", "B", "C" }, r.Valor!.Paradas.Select(p => p.Direccion.Calle));
        Assert.Equal(new[] { 1, 2, 3 }, r.Valor.Paradas.Select(p => p.Orden));
    }

    [Fact]
    public async Task AgregarParada_OrdenFueraDeRango_EsInvalido()
    {
        var v = await _esc.ViajeCreado(Viaje());

        var r = await _esc.Servicio().AgregarParadaAsync(v.Id, 1, Parada("X", orden: 5));

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Fact]
    public async Task AgregarParada_IgualAlDestino_EsInvalido()
    {
        var v = await _esc.ViajeCreado(Viaje());

        var r = await _esc.Servicio().AgregarParadaAsync(
            v.Id, 1, new ParadaDatos(Dir("Av. Hipólito Yrigoyen", 8000, "Lomas de Zamora")));

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Fact]
    public async Task AgregarParada_Repetida_EsInvalido()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("A") }));

        var r = await _esc.Servicio().AgregarParadaAsync(v.Id, 1, Parada("A"));

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Fact]
    public async Task AgregarParada_ConEnviosEnCurso_EsConflicto()
    {
        var v = await _esc.ViajeCreado(Viaje());
        _esc.EnvioAsignado(10, v.Id, EstadoEnvio.ConductorAsignado);

        var r = await _esc.Servicio().AgregarParadaAsync(v.Id, 1, Parada("X"));

        Assert.Equal(TipoError.Conflicto, r.Error);
    }

    [Fact]
    public async Task AgregarParada_ViajeDeOtroUsuario_EsProhibido()
    {
        var v = await _esc.ViajeCreado(Viaje(usuarioId: 1));

        var r = await _esc.Servicio().AgregarParadaAsync(v.Id, 2, Parada("X"));

        Assert.Equal(TipoError.Prohibido, r.Error);
    }

    [Fact]
    public async Task ModificarParada_ConservaElIdYCambiaLaDireccion()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("A"), Parada("B") }));
        var idB = v.Paradas[1].Id;

        var r = await _esc.Servicio().ModificarParadaAsync(v.Id, idB, 1, Dir("B nueva", 77));

        Assert.True(r.Exito, r.Mensaje);
        var b = r.Valor!.Paradas[1];
        Assert.Equal(idB, b.Id);
        Assert.Equal("B nueva", b.Direccion.Calle);
        Assert.Equal(77, b.Direccion.Altura);
    }

    [Fact]
    public async Task ModificarParada_Inexistente_EsNoEncontrado()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("A") }));

        var r = await _esc.Servicio().ModificarParadaAsync(v.Id, 9999, 1, Dir("Z"));

        Assert.Equal(TipoError.NoEncontrado, r.Error);
    }

    [Fact]
    public async Task ModificarParada_QueQuedeIgualAOtra_EsInvalido()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("A"), Parada("B") }));

        var r = await _esc.Servicio().ModificarParadaAsync(v.Id, v.Paradas[1].Id, 1, Dir("A", 500));

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Fact]
    public async Task EliminarParada_RenumeraLasRestantesYLaBorra()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("A"), Parada("B"), Parada("C") }));

        var r = await _esc.Servicio().EliminarParadaAsync(v.Id, v.Paradas[0].Id, 1);

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(new[] { "B", "C" }, r.Valor!.Paradas.Select(p => p.Direccion.Calle));
        Assert.Equal(new[] { 1, 2 }, r.Valor.Paradas.Select(p => p.Orden));
        using var ctx = _esc.Contexto();
        Assert.Equal(2, await ctx.Paradas.CountAsync());
    }

    [Fact]
    public async Task EliminarParada_Inexistente_EsNoEncontrado()
    {
        var v = await _esc.ViajeCreado(Viaje());

        var r = await _esc.Servicio().EliminarParadaAsync(v.Id, 9999, 1);

        Assert.Equal(TipoError.NoEncontrado, r.Error);
    }

    [Fact]
    public async Task ReordenarParadas_PermutacionValida_CambiaElOrdenYConservaIds()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("A"), Parada("B"), Parada("C") }));
        var ids = v.Paradas.Select(p => p.Id).ToList();

        var r = await _esc.Servicio().ReordenarParadasAsync(v.Id, 1, new[] { ids[2], ids[0], ids[1] });

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(new[] { "C", "A", "B" }, r.Valor!.Paradas.Select(p => p.Direccion.Calle));
        Assert.Equal(new[] { ids[2], ids[0], ids[1] }, r.Valor.Paradas.Select(p => p.Id));
    }

    [Fact]
    public async Task ReordenarParadas_ListaIncompletaORepetida_EsInvalido()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("A"), Parada("B") }));
        var id = v.Paradas[0].Id;

        var r = await _esc.Servicio().ReordenarParadasAsync(v.Id, 1, new[] { id, id });

        Assert.Equal(TipoError.Validacion, r.Error);
    }

    [Fact]
    public async Task ListarParadas_DevuelveEnOrden()
    {
        var v = await _esc.ViajeCreado(Viaje(paradas: new[] { Parada("A"), Parada("B") }));

        var r = await _esc.Servicio().ListarParadasAsync(v.Id, 1);

        Assert.True(r.Exito);
        Assert.Equal(new[] { "A", "B" }, r.Valor!.Select(p => p.Direccion.Calle));
    }


    [Fact]
    public async Task CalcularRuta_DevuelveDistanciaYPuntosSinGuardar()
    {
        var r = await _esc.Servicio().CalcularRutaAsync(Dir("A"), Dir("B"), new[] { Parada("P") });

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(20m, r.Valor!.DistanciaKm);
        Assert.Equal(new[] { "Origen", "Parada", "Destino" }, r.Valor.Puntos.Select(p => p.Tipo));
        Assert.Null(r.Valor.Advertencia);
        using var ctx = _esc.Contexto();
        Assert.Empty(ctx.Viajes);
    }

    [Fact]
    public async Task CalcularRuta_MasLargaQueElMaximo_DevuelveAdvertencia()
    {
        _esc.Rutas.DistanciaFija = 120m;

        var r = await _esc.Servicio().CalcularRutaAsync(Dir("A"), Dir("B"), Array.Empty<ParadaDatos>());

        Assert.True(r.Exito);
        Assert.NotNull(r.Valor!.Advertencia);
    }

    [Fact]
    public async Task CalcularRuta_OrigenIgualDestino_EsInvalido()
    {
        var r = await _esc.Servicio().CalcularRutaAsync(Dir("A"), Dir("a"), Array.Empty<ParadaDatos>());

        Assert.Equal(TipoError.Validacion, r.Error);
    }
}
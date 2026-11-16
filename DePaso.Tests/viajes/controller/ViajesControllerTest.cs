using DePaso.Api.Controllers;
using DePaso.Api.Models;
using DePaso.Logica.dtos;
using DePaso.Logica.servicios;
using Microsoft.AspNetCore.Mvc;

namespace DePaso.Tests.viajes.controller;

public class ViajesControllerTests
{
    private sealed class ViajeServicioFalso : IViajeServicio
    {
        public Resultado<ViajeDetalleDto> Detalle { get; set; } = Resultado<ViajeDetalleDto>.Exitoso(DetalleDePrueba());
        public ViajeDatos? DatosRecibidos { get; private set; }
        public FiltroViajes? FiltroRecibido { get; private set; }
        public (int Id, int UsuarioId)? DesactivarRecibido { get; private set; }

        public Task<IReadOnlyList<ViajeResumenDto>> ListarAsync(int usuarioId, FiltroViajes filtro, CancellationToken ct = default)
        {
            FiltroRecibido = filtro;
            return Task.FromResult<IReadOnlyList<ViajeResumenDto>>(new List<ViajeResumenDto>());
        }

        public Task<Resultado<ViajeDetalleDto>> ObtenerAsync(int id, int usuarioId, CancellationToken ct = default) =>
            Task.FromResult(Detalle);

        public Task<Resultado<ViajeDetalleDto>> CrearAsync(ViajeDatos datos, CancellationToken ct = default)
        {
            DatosRecibidos = datos;
            return Task.FromResult(Detalle);
        }

        public Task<Resultado<ViajeDetalleDto>> ActualizarAsync(int id, ViajeDatos datos, CancellationToken ct = default)
        {
            DatosRecibidos = datos;
            return Task.FromResult(Detalle);
        }

        public Task<Resultado<ViajeDetalleDto>> DesactivarAsync(int id, int usuarioId, CancellationToken ct = default)
        {
            DesactivarRecibido = (id, usuarioId);
            return Task.FromResult(Detalle);
        }

        public Task<Resultado<ViajeDetalleDto>> ActivarAsync(int id, int usuarioId, CancellationToken ct = default) =>
            Task.FromResult(Detalle);

        public Task<Resultado<RutaDto>> CalcularRutaAsync(
            DireccionDatos origen,
            DireccionDatos destino,
            IReadOnlyList<ParadaDatos> paradas,
            CancellationToken ct = default) =>
            Task.FromResult(Resultado<RutaDto>.Exitoso(new RutaDto(12.5m, 20, true, new List<PuntoRutaDto>(), null)));

        public Task<Resultado<IReadOnlyList<ParadaDto>>> ListarParadasAsync(int id, int usuarioId, CancellationToken ct = default) =>
            Task.FromResult(Resultado<IReadOnlyList<ParadaDto>>.Exitoso(new List<ParadaDto>()));

        public Task<Resultado<ViajeDetalleDto>> AgregarParadaAsync(int id, int usuarioId, ParadaDatos parada, CancellationToken ct = default) =>
            Task.FromResult(Detalle);

        public Task<Resultado<ViajeDetalleDto>> ModificarParadaAsync(
            int id,
            int paradaId,
            int usuarioId,
            DireccionDatos direccion,
            CancellationToken ct = default) =>
            Task.FromResult(Detalle);

        public Task<Resultado<ViajeDetalleDto>> EliminarParadaAsync(int id, int paradaId, int usuarioId, CancellationToken ct = default) =>
            Task.FromResult(Detalle);

        public Task<Resultado<ViajeDetalleDto>> ReordenarParadasAsync(
            int id,
            int usuarioId,
            IReadOnlyList<int> paradaIds,
            CancellationToken ct = default) =>
            Task.FromResult(Detalle);

        public Task<Resultado<IReadOnlyList<PaqueteAsignadoDto>>> ListarPaquetesAsync(int id, int usuarioId, CancellationToken ct = default) =>
            Task.FromResult(Resultado<IReadOnlyList<PaqueteAsignadoDto>>.Exitoso(new List<PaqueteAsignadoDto>()));
    }

    private static ViajeDetalleDto DetalleDePrueba()
    {
        var dir = new DireccionDto("Calle", 1, null, null, "CABA", "Buenos Aires", -34.6m, -58.4m, "Calle 1, CABA, Buenos Aires");
        return new ViajeDetalleDto(
            7, 1, dir, dir with { Calle = "Otra" }, new List<byte> { 1 }, "Lun",
            new TimeOnly(8, 0), new TimeOnly(9, 0), 3, 10, 5, 20, 30, true,
            new List<ParadaDto>(), new List<PaqueteAsignadoDto>());
    }

    private static ViajeAlta ModeloDePrueba() => new()
    {
        UsuarioId = 1,
        Origen = new DireccionViaje { Calle = "Av. Cabildo", Altura = 1000, Localidad = "CABA", Provincia = "Buenos Aires" },
        Destino = new DireccionViaje { Calle = "Av. Mitre", Altura = 300, Localidad = "Avellaneda", Provincia = "Buenos Aires", Lat = -34.66m, Lng = -58.36m },
        Dias = new List<byte> { 1, 5 },
        HoraSalida = new TimeOnly(8, 0),
        HoraLlegada = new TimeOnly(9, 0),
        MaxPaquetes = 3,
        PesoMaxKg = 10,
        DesvioMaxKm = 5,
        Paradas = new List<ParadaViaje>
            {
                new() { Calle = "Av. Santa Fe", Altura = 500, Localidad = "CABA", Provincia = "Buenos Aires", Orden = 1 },
            },
    };

    private readonly ViajeServicioFalso _servicio = new();
    private ViajesController Sut() => new(_servicio);

    [Theory]
    [InlineData(null, FiltroViajes.Todos)]
    [InlineData("todos", FiltroViajes.Todos)]
    [InlineData("ACTIVOS", FiltroViajes.Activos)]
    [InlineData("inactivos", FiltroViajes.Inactivos)]
    public async Task Listar_ConFiltroValido_Devuelve200YPasaElFiltro(string? estado, FiltroViajes esperado)
    {
        var r = await Sut().Listar(1, estado, CancellationToken.None);

        Assert.IsType<OkObjectResult>(r);
        Assert.Equal(esperado, _servicio.FiltroRecibido);
    }

    [Fact]
    public async Task Listar_ConFiltroInvalido_Devuelve400()
    {
        var r = await Sut().Listar(1, "cualquiera", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(r);
        Assert.Null(_servicio.FiltroRecibido);
    }

    [Fact]
    public async Task Obtener_Exitoso_Devuelve200ConElDetalle()
    {
        var r = await Sut().Obtener(7, 1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(r);
        Assert.Equal(7, Assert.IsType<ViajeDetalleDto>(ok.Value).Id);
    }

    [Fact]
    public async Task Obtener_NoEncontrado_Devuelve404()
    {
        _servicio.Detalle = Resultado<ViajeDetalleDto>.Fallo(TipoError.NoEncontrado, "Viaje no encontrado.");

        var r = await Sut().Obtener(7, 1, CancellationToken.None);

        var nf = Assert.IsType<NotFoundObjectResult>(r);
        Assert.Equal("Viaje no encontrado.", Assert.IsType<ErrorViajeRespuesta>(nf.Value).Mensaje);
    }

    [Fact]
    public async Task Obtener_DeOtroUsuario_Devuelve403()
    {
        _servicio.Detalle = Resultado<ViajeDetalleDto>.Fallo(TipoError.Prohibido, "El viaje pertenece a otro usuario.");

        var r = await Sut().Obtener(7, 2, CancellationToken.None);

        Assert.Equal(403, Assert.IsType<ObjectResult>(r).StatusCode);
    }

    [Fact]
    public async Task Crear_Exitoso_Devuelve201ConRutaAlDetalle()
    {
        var r = await Sut().Crear(ModeloDePrueba(), CancellationToken.None);

        var creado = Assert.IsType<CreatedAtRouteResult>(r);
        Assert.Equal("ObtenerViaje", creado.RouteName);
        Assert.Equal(7, creado.RouteValues!["id"]);
        Assert.Equal(1, creado.RouteValues["usuarioId"]);
    }

    [Fact]
    public async Task Crear_MapeaElModeloAlServicio()
    {
        await Sut().Crear(ModeloDePrueba(), CancellationToken.None);

        var d = _servicio.DatosRecibidos!;
        Assert.Equal(1, d.UsuarioId);
        Assert.Equal("Av. Cabildo", d.Origen.Calle);
        Assert.Null(d.Origen.Lat);
        Assert.Equal(-34.66m, d.Destino.Lat);
        Assert.Equal(new byte[] { 1, 5 }, d.Dias);
        Assert.Equal(new TimeOnly(8, 0), d.HoraSalida);
        var parada = Assert.Single(d.Paradas);
        Assert.Equal("Av. Santa Fe", parada.Direccion.Calle);
        Assert.Equal(1, parada.Orden);
    }

    [Fact]
    public async Task Crear_Invalido_Devuelve400ConLosErrores()
    {
        _servicio.Detalle = Resultado<ViajeDetalleDto>.Invalido(new[] { "Error 1.", "Error 2." });

        var r = await Sut().Crear(ModeloDePrueba(), CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(r);
        Assert.Equal(new[] { "Error 1.", "Error 2." }, Assert.IsType<ErrorViajeRespuesta>(bad.Value).Errores);
    }

    [Fact]
    public async Task Actualizar_Conflicto_Devuelve409()
    {
        _servicio.Detalle = Resultado<ViajeDetalleDto>.Fallo(TipoError.Conflicto, "Tiene envíos en curso.");

        var r = await Sut().Actualizar(7, ModeloDePrueba(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(r);
    }

    [Fact]
    public async Task Desactivar_Exitoso_Devuelve200YPasaIdYUsuario()
    {
        var r = await Sut().Desactivar(7, 1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(r);
        Assert.Equal((7, 1), _servicio.DesactivarRecibido);
    }

    [Fact]
    public async Task Desactivar_YaInactivo_Devuelve409()
    {
        _servicio.Detalle = Resultado<ViajeDetalleDto>.Fallo(TipoError.Conflicto, "El viaje ya está inactivo.");

        var r = await Sut().Desactivar(7, 1, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(r);
    }

    [Fact]
    public async Task CalcularRuta_Devuelve200()
    {
        var modelo = new CalcularRutaModelo { Origen = ModeloDePrueba().Origen, Destino = ModeloDePrueba().Destino };

        var r = await Sut().CalcularRuta(modelo, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(r);
        Assert.Equal(12.5m, Assert.IsType<RutaDto>(ok.Value).DistanciaKm);
    }

    [Fact]
    public async Task AgregarParada_Exitoso_Devuelve201()
    {
        var modelo = new ParadaAlta { UsuarioId = 1, Calle = "X", Altura = 1, Localidad = "CABA", Provincia = "Buenos Aires" };

        var r = await Sut().AgregarParada(7, modelo, CancellationToken.None);

        Assert.IsType<CreatedAtRouteResult>(r);
    }

    [Fact]
    public async Task EliminarParada_NoEncontrada_Devuelve404()
    {
        _servicio.Detalle = Resultado<ViajeDetalleDto>.Fallo(TipoError.NoEncontrado, "Parada no encontrada en este viaje.");

        var r = await Sut().EliminarParada(7, 99, 1, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(r);
    }

    [Fact]
    public async Task ReordenarParadas_Invalido_Devuelve400()
    {
        _servicio.Detalle = Resultado<ViajeDetalleDto>.Invalido("Enviá todas las paradas.");

        var r = await Sut().ReordenarParadas(
            7, new ReordenarParadas { UsuarioId = 1, ParadaIds = new List<int> { 1 } }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(r);
    }
}
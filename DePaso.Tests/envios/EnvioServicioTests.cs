using DePaso.Entidades;
using DePaso.Logica;
using DePaso.Logica.enums;
using DePaso.Tests.dashboard.helpers;

namespace DePaso.Tests.envios;

public class EnvioServicioTests
{
    [Fact]
    public async Task Crear_SinUsuario_NoGuarda()
    {
        await using var db = Db.Nuevo();
        var servicio = new EnvioServicio(db);

        var envio = await Crear(servicio, 1);

        Assert.Null(envio);
        Assert.Empty(db.Envios);
    }

    [Fact]
    public async Task Crear_GuardaBuscandoConductorYRecortaLaDescripcion()
    {
        await using var db = Db.Nuevo();
        Sembrar.Usuario(db, 1);
        var servicio = new EnvioServicio(db);

        var envio = await Crear(servicio, 1, "  Caja  ", 4500);

        Assert.NotNull(envio);
        Assert.Equal((byte)EstadoEnvio.BuscandoConductor, envio.Estado);
        Assert.Equal("Caja", envio.Descripcion);
        Assert.Equal(4500, envio.Monto);
        Assert.Equal(1, db.Envios.Count());
    }

    [Fact]
    public async Task Editar_BuscandoConductor_Actualiza()
    {
        await using var db = Db.Nuevo();
        Sembrar.Usuario(db, 1);
        var servicio = new EnvioServicio(db);
        var creado = (await Crear(servicio, 1, "Caja", 1000))!;

        var editado = await Editar(servicio, creado.Id, 1, "Sobre", 2500);

        Assert.NotNull(editado);
        Assert.Equal("Sobre", editado.Descripcion);
        Assert.Equal(2500, editado.Monto);
        Assert.Equal((byte)EstadoEnvio.BuscandoConductor, editado.Estado);
    }

    [Fact]
    public async Task Editar_OtroEstado_NoCambia()
    {
        await using var db = Db.Nuevo();
        Sembrar.Usuario(db, 1);
        var servicio = new EnvioServicio(db);
        var creado = (await Crear(servicio, 1, "Caja", 1000))!;
        creado.Estado = (byte)EstadoEnvio.EnCamino;
        await db.SaveChangesAsync();

        var editado = await Editar(servicio, creado.Id, 1, "Sobre", 2500);

        Assert.Null(editado);
        Assert.Equal("Caja", db.Envios.Single().Descripcion);
        Assert.Equal((byte)EstadoEnvio.EnCamino, db.Envios.Single().Estado);
    }

    [Fact]
    public async Task Editar_DeOtroUsuario_NoCambia()
    {
        await using var db = Db.Nuevo();
        Sembrar.Usuario(db, 1);
        Sembrar.Usuario(db, 2);
        var servicio = new EnvioServicio(db);
        var creado = (await Crear(servicio, 1, "Caja", 1000))!;

        var editado = await Editar(servicio, creado.Id, 2, "Sobre", 2500);

        Assert.Null(editado);
        Assert.Equal("Caja", db.Envios.Single().Descripcion);
    }

    [Fact]
    public async Task Borrar_BuscandoConductor_CancelaYGuardaHistoria()
    {
        await using var db = Db.Nuevo();
        Sembrar.Usuario(db, 1);
        var servicio = new EnvioServicio(db);
        var creado = (await Crear(servicio, 1))!;

        var borro = await servicio.Borrar(creado.Id, 1);

        Assert.True(borro);
        Assert.Equal((byte)EstadoEnvio.Cancelado, db.Envios.Single().Estado);
        var historia = db.EnvioEstadoHistoria.Single();
        Assert.Equal(creado.Id, historia.EnvioId);
        Assert.Equal((byte)EstadoEnvio.BuscandoConductor, historia.EstadoAnterior);
        Assert.Equal((byte)EstadoEnvio.Cancelado, historia.EstadoNuevo);
    }

    [Fact]
    public async Task Borrar_OtroEstado_NoCancela()
    {
        await using var db = Db.Nuevo();
        Sembrar.Usuario(db, 1);
        var servicio = new EnvioServicio(db);
        var creado = (await Crear(servicio, 1))!;
        creado.Estado = (byte)EstadoEnvio.Entregado;
        await db.SaveChangesAsync();

        var borro = await servicio.Borrar(creado.Id, 1);

        Assert.False(borro);
        Assert.Equal((byte)EstadoEnvio.Entregado, db.Envios.Single().Estado);
        Assert.Empty(db.EnvioEstadoHistoria);
    }

    [Fact]
    public async Task Borrar_DeOtroUsuario_NoCancela()
    {
        await using var db = Db.Nuevo();
        Sembrar.Usuario(db, 1);
        Sembrar.Usuario(db, 2);
        var servicio = new EnvioServicio(db);
        var creado = (await Crear(servicio, 1))!;

        var borro = await servicio.Borrar(creado.Id, 2);

        Assert.False(borro);
        Assert.Equal((byte)EstadoEnvio.BuscandoConductor, db.Envios.Single().Estado);
    }

    private static Task<Envios?> Crear(
        EnvioServicio servicio,
        int usuarioId,
        string descripcion = "Caja",
        decimal monto = 1000
    ) =>
        servicio.Crear(
            usuarioId,
            "Cabildo",
            2000,
            null,
            null,
            "Belgrano",
            "CABA",
            -34.56m,
            -58.45m,
            "Yrigoyen",
            2000,
            null,
            null,
            "Lomas de Zamora",
            "Buenos Aires",
            -34.67m,
            -58.55m,
            new DateOnly(2026, 12, 20),
            2m,
            1,
            descripcion,
            monto
        );

    private static Task<Envios?> Editar(
        EnvioServicio servicio,
        int id,
        int usuarioId,
        string descripcion,
        decimal monto
    ) =>
        servicio.Editar(
            id,
            usuarioId,
            "Cabildo",
            2000,
            null,
            null,
            "Belgrano",
            "CABA",
            -34.56m,
            -58.45m,
            "Yrigoyen",
            2000,
            null,
            null,
            "Lomas de Zamora",
            "Buenos Aires",
            -34.67m,
            -58.55m,
            new DateOnly(2026, 12, 20),
            2m,
            1,
            descripcion,
            monto
        );
}

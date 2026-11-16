using DePaso.Logica;
using DePaso.Tests.dashboard.helpers;

namespace DePaso.Tests.cuenta;

public class UsuarioServicioTests
{
    [Fact]
    public async Task Registrar_PersisteElUsuarioYNoGuardaLaContrasenaEnTexto()
    {
        await using var db = Db.Nuevo();
        var servicio = new UsuarioServicio(db);

        var (usuario, error) = await servicio.Registrar("Ana", "López", " ana@mail.com ", "Clave1234");

        Assert.Null(error);
        Assert.NotNull(usuario);
        Assert.Equal("ana@mail.com", usuario.Email);
        Assert.Equal("Ana", usuario.Nombre);
        Assert.DoesNotContain("Clave1234", usuario.PasswordHash);
        Assert.Contains('.', usuario.PasswordHash);
        Assert.Equal(1, db.Usuarios.Count());
    }

    [Fact]
    public async Task Registrar_MailRepetido_NoCreaOtroUsuario()
    {
        await using var db = Db.Nuevo();
        var servicio = new UsuarioServicio(db);
        await servicio.Registrar("Ana", "López", "ana@mail.com", "Clave1234");

        var (usuario, error) = await servicio.Registrar("Otro", "Nombre", "ANA@mail.com", "OtraClave1");

        Assert.Null(usuario);
        Assert.Equal("Ese email ya está registrado.", error);
        Assert.Equal(1, db.Usuarios.Count());
    }

    [Fact]
    public async Task Ingresar_ContrasenaCorrecta_DevuelveElUsuario()
    {
        await using var db = Db.Nuevo();
        var servicio = new UsuarioServicio(db);
        await servicio.Registrar("Ana", "López", "ana@mail.com", "Clave1234");

        var usuario = await servicio.Ingresar(" ANA@mail.com ", "Clave1234");

        Assert.NotNull(usuario);
        Assert.Equal("ana@mail.com", usuario.Email);
    }

    [Fact]
    public async Task Ingresar_ContrasenaIncorrecta_DevuelveNull()
    {
        await using var db = Db.Nuevo();
        var servicio = new UsuarioServicio(db);
        await servicio.Registrar("Ana", "López", "ana@mail.com", "Clave1234");

        var usuario = await servicio.Ingresar("ana@mail.com", "ClaveDistinta");

        Assert.Null(usuario);
    }

    [Fact]
    public async Task Ingresar_MailInexistente_DevuelveNull()
    {
        await using var db = Db.Nuevo();
        var servicio = new UsuarioServicio(db);

        var usuario = await servicio.Ingresar("nadie@mail.com", "Clave1234");

        Assert.Null(usuario);
    }
}

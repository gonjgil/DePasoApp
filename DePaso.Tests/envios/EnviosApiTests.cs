using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DePaso.Api.Models;
using DePaso.Entidades;
using DePaso.Logica.enums;
using DePaso.Tests.dashboard.helpers;
using DePaso.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ApiEnviosController = DePaso.Api.Controllers.EnviosController;

namespace DePaso.Tests.envios;

public sealed class EnviosApiTests : IDisposable
{
    private readonly ApiDePrueba _api = new();
    private readonly HttpClient _cliente;

    public EnviosApiTests()
    {
        _cliente = _api.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task MisEnvios_DevuelveSoloLosEnviosDelUsuario()
    {
        SembrarEnvios();

        var envios = await _cliente.GetFromJsonAsync<List<EnvioApiResponse>>(
            "/api/envios/mios?usuarioId=7");

        var envio = Assert.Single(Assert.IsType<List<EnvioApiResponse>>(envios));
        Assert.Equal(31, envio.Id);
        Assert.Equal(7, envio.UsuarioId);
    }

    [Fact]
    public async Task Crear_ConDatosValidos_Devuelve201YGuardaElEnvio()
    {
        using var alcance = _api.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<DePasoAppContext>();
        Sembrar.Usuario(contexto, 7);

        var modelo = Registro();
        modelo.OrigenDir = "  Morón  ";
        using var respuesta = await _cliente.PostAsJsonAsync("/api/envios/crear", modelo);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var envio = Assert.IsType<EnvioApiResponse>(
            await respuesta.Content.ReadFromJsonAsync<EnvioApiResponse>());
        Assert.True(envio.Id > 0);
        Assert.Equal(7, envio.UsuarioId);
        Assert.Equal("Morón", envio.OrigenDir);
        Assert.Equal((byte)EstadoEnvio.BuscandoConductor, envio.Estado);

        var guardado = Assert.IsType<EnvioApiResponse>(
            await _cliente.GetFromJsonAsync<EnvioApiResponse>($"/api/envios/{envio.Id}?usuarioId=7"));
        Assert.Equal(modelo.DestinoDir, guardado.DestinoDir);
        Assert.Equal(modelo.PesoKg, guardado.PesoKg);
    }

    [Fact]
    public async Task Crear_ConOrigenVacio_Devuelve400ConElErrorDelCampo()
    {
        var modelo = Registro();
        modelo.OrigenDir = "";

        using var respuesta = await _cliente.PostAsJsonAsync("/api/envios/crear", modelo);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problema.GetProperty("errors").TryGetProperty(nameof(EnvioRegistro.OrigenDir), out _));
    }

    [Fact]
    public async Task Crear_ConUsuarioInexistente_Devuelve404()
    {
        using var respuesta = await _cliente.PostAsJsonAsync("/api/envios/crear", Registro());

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(31, 8)]
    [InlineData(999, 7)]
    public async Task ObtenerPorId_ConEnvioAjenoOInexistente_Devuelve404(int id, int usuarioId)
    {
        SembrarEnvios();

        using var respuesta = await _cliente.GetAsync($"/api/envios/{id}?usuarioId={usuarioId}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Editar_ActualizaElDestinoDelEnvio()
    {
        SembrarEnvios();
        var modelo = Registro();
        modelo.DestinoDir = "Ituzaingó";

        using var respuesta = await _cliente.PutAsJsonAsync("/api/envios/31", modelo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var guardado = Assert.IsType<EnvioApiResponse>(
            await _cliente.GetFromJsonAsync<EnvioApiResponse>("/api/envios/31?usuarioId=7"));
        Assert.Equal("Ituzaingó", guardado.DestinoDir);
    }

    [Fact]
    public async Task Borrar_EliminaElEnvio()
    {
        SembrarEnvios();

        using var respuesta = await _cliente.DeleteAsync("/api/envios/31?usuarioId=7");
        using var consulta = await _cliente.GetAsync("/api/envios/31?usuarioId=7");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _api.Dispose();
    }

    private void SembrarEnvios()
    {
        using var alcance = _api.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<DePasoAppContext>();
        Sembrar.Usuario(contexto, 7);
        Sembrar.Usuario(contexto, 8);
        Sembrar.Envio(contexto, 31, 7, 5000m);
        Sembrar.Envio(contexto, 32, 8, 4000m);
    }

    private static EnvioRegistro Registro() => new()
    {
        UsuarioId = 7,
        OrigenDir = "Morón",
        DestinoDir = "Moreno",
        FechaLimite = new DateOnly(2030, 3, 15),
        PesoKg = 2.5m,
        Tamanio = 1,
        Descripcion = "Una caja",
        Monto = 5000m
    };

    private sealed class ApiDePrueba : WebApplicationFactory<ApiEnviosController>
    {
        private readonly string _nombreBase = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<DePasoAppContext>();
                servicios.AddScoped<DePasoAppContext>(_ => Db.Nuevo(_nombreBase));
            });
        }
    }
}

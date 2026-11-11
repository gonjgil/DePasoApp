using System.Net;
using System.Security.Claims;
using System.Text.Json;
using DePaso.Web.Controllers;
using DePaso.Web.Models;
using DePaso.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DePaso.Tests.envios;

public sealed class EnviosControllerTests
{
    [Fact]
    public async Task Index_EnviaElUsuarioALaApiYDevuelveLaVistaConSusEnvios()
    {
        var api = new EnviosHttpFalso(HttpStatusCode.OK, new[] { new EnvioApiResponse { Id = 31 } });
        using var http = CrearCliente(api);
        var controller = CrearController(http);

        var resultado = await controller.Index();

        var vista = Assert.IsType<ViewResult>(resultado);
        var envio = Assert.Single(Assert.IsType<List<EnvioApiResponse>>(vista.Model));
        Assert.Equal(31, envio.Id);
        Assert.Equal(HttpMethod.Get, api.Metodo);
        Assert.Equal("/api/envios/mios?usuarioId=7", api.Ruta);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    public async Task Index_ConIdentificadorInvalido_RedirigeAIngresar(string? usuarioId)
    {
        var api = new EnviosHttpFalso(HttpStatusCode.OK);
        using var http = CrearCliente(api);
        var controller = CrearController(http, usuarioId);

        var resultado = await controller.Index();

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Cuenta", redireccion.ControllerName);
        Assert.Equal("Ingresar", redireccion.ActionName);
        Assert.Null(api.Metodo);
    }

    [Fact]
    public async Task Nuevo_ConModeloInvalido_VuelveAlFormularioConLosDatos()
    {
        var api = new EnviosHttpFalso(HttpStatusCode.Created);
        using var http = CrearCliente(api);
        var controller = CrearController(http);
        var modelo = new NuevoEnvioViewModel { DestinoDir = "Moreno" };
        controller.ModelState.AddModelError(nameof(modelo.OrigenDir), "Ingresá una dirección de origen.");

        var resultado = await controller.Nuevo(modelo);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        Assert.Null(api.Metodo);
    }

    [Fact]
    public async Task Nuevo_SiLaApiCreaElEnvio_RedirigeAlListado()
    {
        var api = new EnviosHttpFalso(HttpStatusCode.Created, new EnvioApiResponse { Id = 31 });
        using var http = CrearCliente(api);
        var controller = CrearController(http);

        var resultado = await controller.Nuevo(ModeloValido());

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(EnviosController.Index), redireccion.ActionName);
        Assert.Equal(HttpMethod.Post, api.Metodo);
        Assert.Equal("/api/envios/crear", api.Ruta);
        var cuerpo = Assert.IsType<string>(api.Cuerpo);
        using var json = JsonDocument.Parse(cuerpo);
        Assert.Equal(7, json.RootElement.GetProperty("usuarioId").GetInt32());
        Assert.Equal("Moreno", json.RootElement.GetProperty("destinoDir").GetString());
    }

    [Fact]
    public async Task Nuevo_SiLaApiRechazaElEnvio_VuelveAlFormularioConElError()
    {
        var api = new EnviosHttpFalso(HttpStatusCode.NotFound);
        using var http = CrearCliente(api);
        var controller = CrearController(http);
        var modelo = ModeloValido();

        var resultado = await controller.Nuevo(modelo);

        var vista = Assert.IsType<ViewResult>(resultado);
        Assert.Same(modelo, vista.Model);
        var error = Assert.Single(controller.ModelState[string.Empty]!.Errors);
        Assert.Equal("Usuario no encontrado.", error.ErrorMessage);
    }

    [Fact]
    public async Task Editar_SiLaApiNoEncuentraElEnvio_RedirigeAlListado()
    {
        var api = new EnviosHttpFalso(HttpStatusCode.NotFound);
        using var http = CrearCliente(api);
        var controller = CrearController(http);

        var resultado = await controller.Editar(31);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(EnviosController.Index), redireccion.ActionName);
        Assert.Equal(HttpMethod.Get, api.Metodo);
        Assert.Equal("/api/envios/31?usuarioId=7", api.Ruta);
    }

    [Fact]
    public async Task Editar_SiLaApiGuardaElCambio_RedirigeAlListado()
    {
        var api = new EnviosHttpFalso(HttpStatusCode.OK, new EnvioApiResponse { Id = 31 });
        using var http = CrearCliente(api);
        var controller = CrearController(http);
        var modelo = ModeloValido();
        modelo.DestinoDir = "Ituzaingó";

        var resultado = await controller.Editar(31, modelo);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(EnviosController.Index), redireccion.ActionName);
        Assert.Equal(HttpMethod.Put, api.Metodo);
        Assert.Equal("/api/envios/31", api.Ruta);
        var cuerpo = Assert.IsType<string>(api.Cuerpo);
        using var json = JsonDocument.Parse(cuerpo);
        Assert.Equal("Ituzaingó", json.RootElement.GetProperty("destinoDir").GetString());
    }

    [Fact]
    public async Task Borrar_SolicitaElBorradoYRedirigeAlListado()
    {
        var api = new EnviosHttpFalso(HttpStatusCode.OK);
        using var http = CrearCliente(api);
        var controller = CrearController(http);

        var resultado = await controller.Borrar(31);

        var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(EnviosController.Index), redireccion.ActionName);
        Assert.Equal(HttpMethod.Delete, api.Metodo);
        Assert.Equal("/api/envios/31?usuarioId=7", api.Ruta);
    }

    private static HttpClient CrearCliente(EnviosHttpFalso api) => new(api)
    {
        BaseAddress = new Uri("https://api.depaso.test/")
    };

    private static EnviosController CrearController(HttpClient http, string? usuarioId = "7")
    {
        Claim[] claims = usuarioId is null ? [] : [new Claim(ClaimTypes.NameIdentifier, usuarioId)];
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"));

        return new EnviosController(new EnviosApiClient(http))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = usuario }
            }
        };
    }

    private static NuevoEnvioViewModel ModeloValido() => new()
    {
        OrigenDir = "Morón",
        DestinoDir = "Moreno",
        FechaLimite = new DateOnly(2030, 3, 15),
        PesoKg = 2.5m,
        Tamanio = 1,
        Descripcion = "Una caja",
        Monto = 5000m
    };
}

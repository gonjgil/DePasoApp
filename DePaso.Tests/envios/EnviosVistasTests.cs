using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DePaso.Web.Controllers;
using DePaso.Web.Models;
using DePaso.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DePaso.Tests.envios;

public sealed class EnviosVistasTests
{
    [Fact]
    public async Task Index_SinEnvios_MuestraElMensajeDeListaVacia()
    {
        using var api = new EnviosHttpFalso(HttpStatusCode.OK, Array.Empty<EnvioApiResponse>());

        var html = await ObtenerHtml("/Envios", api);

        Assert.Contains("Todavía no tenés envíos cargados.", html);
        Assert.Contains("href=\"/Envios/Nuevo\"", html);
    }

    [Fact]
    public async Task Index_ConUnEnvio_MuestraLosDatosYLasAcciones()
    {
        using var api = new EnviosHttpFalso(HttpStatusCode.OK, new[] { EnvioDePrueba() });

        var html = await ObtenerHtml("/Envios", api);

        Assert.Contains("<td>Morón</td>", html);
        Assert.Contains("<td>Moreno</td>", html);
        Assert.Contains("href=\"/Envios/Editar/31\"", html);
        Assert.Contains("action=\"/Envios/Borrar\"", html);
        Assert.Contains("name=\"id\" value=\"31\"", html);
    }

    [Fact]
    public async Task Nuevo_MuestraElFormularioDeAlta()
    {
        using var api = new EnviosHttpFalso(HttpStatusCode.OK);

        var html = await ObtenerHtml("/Envios/Nuevo", api);

        Assert.Contains("<h1>Nuevo envío</h1>", html);
        Assert.Contains("action=\"/Envios/Nuevo\"", html);
        Assert.Contains("name=\"OrigenDir\"", html);
        Assert.Contains("name=\"DestinoDir\"", html);
    }

    [Fact]
    public async Task Editar_MuestraElFormularioConLosDatosGuardados()
    {
        using var api = new EnviosHttpFalso(HttpStatusCode.OK, EnvioDePrueba());

        var html = await ObtenerHtml("/Envios/Editar/31", api);

        Assert.Contains("<h1>Editar envío</h1>", html);
        Assert.Contains("action=\"/Envios/Editar/31\"", html);
        Assert.Matches("""<input\b(?=[^>]*\bname="OrigenDir")(?=[^>]*\bvalue="Morón")[^>]*>""", html);
        Assert.Matches("""<input\b(?=[^>]*\bname="DestinoDir")(?=[^>]*\bvalue="Moreno")[^>]*>""", html);
    }

    private static async Task<string> ObtenerHtml(string ruta, EnviosHttpFalso api)
    {
        using var app = new WebDePrueba(api);
        using var cliente = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        using var respuesta = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
    }

    private static EnvioApiResponse EnvioDePrueba() => new()
    {
        Id = 31,
        UsuarioId = 7,
        OrigenDir = "Morón",
        DestinoDir = "Moreno",
        FechaLimite = new DateOnly(2030, 3, 15),
        PesoKg = 2.5m,
        Tamanio = 1,
        Descripcion = "Una caja",
        Monto = 5000m
    };

    private sealed class WebDePrueba(EnviosHttpFalso api) : WebApplicationFactory<EnviosController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuracion) =>
            {
                configuracion.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiUsuariosUrl"] = "https://api.depaso.test/"
                });
            });

            builder.ConfigureTestServices(servicios =>
            {
                const string esquema = "UsuarioDePrueba";
                servicios.AddAuthentication(opciones =>
                {
                    opciones.DefaultAuthenticateScheme = esquema;
                    opciones.DefaultChallengeScheme = esquema;
                }).AddScheme<AuthenticationSchemeOptions, UsuarioDePrueba>(esquema, _ => { });

                servicios.AddHttpClient<EnviosApiClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => api);
            });
        }
    }

    private sealed class UsuarioDePrueba(
        IOptionsMonitor<AuthenticationSchemeOptions> opciones,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Claim[] claims = [new Claim(ClaimTypes.NameIdentifier, "7")];
            var usuario = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(usuario, Scheme.Name)));
        }
    }
}

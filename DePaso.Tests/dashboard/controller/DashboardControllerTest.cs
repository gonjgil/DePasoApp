using DePaso.Logica.Dtos;
using DePaso.Logica.servicios;
using DePaso.Web.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace DePaso.Tests.dashboard.controller
{
    public class DashboardControllerTests
    {
        private sealed class ServicioDashboardFalso : IDashboardServicio
        {
            public DashboardResumenDto? Respuesta { get; set; }
            public int? UsuarioIdRecibido { get; private set; }

            public Task<DashboardResumenDto?> ObtenerResumenAsync(int usuarioId, CancellationToken cancelacion = default)
            {
                UsuarioIdRecibido = usuarioId;
                return Task.FromResult(Respuesta);
            }
        }

        private sealed class AutenticacionFalsa : IAuthenticationService
        {
            public bool SignOutLlamado { get; private set; }
            public string? Esquema { get; private set; }

            public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
                Task.FromResult(AuthenticateResult.NoResult());

            public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;

            public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;

            public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
                Task.CompletedTask;

            public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            {
                SignOutLlamado = true;
                Esquema = scheme;
                return Task.CompletedTask;
            }
        }

        private sealed class TempDataProviderFalso : ITempDataProvider
        {
            public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();

            public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
            {
            }
        }

        private sealed class UrlHelperFalso : IUrlHelper
        {
            public ActionContext ActionContext { get; } = new();

            public string? Action(UrlActionContext actionContext) => "/";

            public string? Content(string? contentPath) => contentPath;

            public bool IsLocalUrl(string? url) => true;

            public string? RouteUrl(UrlRouteContext routeContext) => "/";

            public string? Link(string? routeName, object? values) => "/";
        }

        private static DashboardResumenDto ResumenVacio() => new(
            "Juan", 0, 0, 0, 0m, Array.Empty<ViajeProximoDto>(), Array.Empty<EnvioPendienteDto>());

        private static ClaimsPrincipal Usuario(string? id)
        {
            var claims = id == null ? new List<Claim>() : new List<Claim> { new(ClaimTypes.NameIdentifier, id) };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        private static DashboardController Crear(IDashboardServicio servicio, AutenticacionFalsa auth, ClaimsPrincipal usuario)
        {
            var http = new DefaultHttpContext
            {
                User = usuario,
                RequestServices = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider()
            };
            return new DashboardController(servicio)
            {
                ControllerContext = new ControllerContext { HttpContext = http },
                TempData = new TempDataDictionary(http, new TempDataProviderFalso()),
                Url = new UrlHelperFalso()
            };
        }

        [Fact]
        public void ElControllerExigeAutenticacion()
        {
            Assert.NotEmpty(typeof(DashboardController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        }

        [Fact]
        public async Task Index_ConUsuarioValido_DevuelveLaVistaConElResumen()
        {
            var servicio = new ServicioDashboardFalso { Respuesta = ResumenVacio() };
            var auth = new AutenticacionFalsa();
            var controller = Crear(servicio, auth, Usuario("7"));

            var resultado = await controller.Index(CancellationToken.None);

            var vista = Assert.IsType<ViewResult>(resultado);
            Assert.Same(servicio.Respuesta, vista.Model);
            Assert.Equal(7, servicio.UsuarioIdRecibido);
            Assert.False(auth.SignOutLlamado);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("no-es-un-numero")]
        public async Task Index_ConClaimInvalido_CierraSesionYVuelveAIngresar(string? claim)
        {
            var servicio = new ServicioDashboardFalso { Respuesta = ResumenVacio() };
            var auth = new AutenticacionFalsa();
            var controller = Crear(servicio, auth, Usuario(claim));

            var resultado = await controller.Index(CancellationToken.None);

            AssertVuelveAIngresar(resultado, auth);
            Assert.Null(servicio.UsuarioIdRecibido);
        }

        [Fact]
        public async Task Index_SiElUsuarioYaNoExiste_CierraSesionYVuelveAIngresar()
        {
            var servicio = new ServicioDashboardFalso { Respuesta = null };
            var auth = new AutenticacionFalsa();
            var controller = Crear(servicio, auth, Usuario("7"));

            var resultado = await controller.Index(CancellationToken.None);

            AssertVuelveAIngresar(resultado, auth);
            Assert.Equal(7, servicio.UsuarioIdRecibido);
        }

        private static void AssertVuelveAIngresar(IActionResult resultado, AutenticacionFalsa auth)
        {
            var redireccion = Assert.IsType<RedirectToActionResult>(resultado);
            Assert.Equal("Ingresar", redireccion.ActionName);
            Assert.Equal("Cuenta", redireccion.ControllerName);
            Assert.True(auth.SignOutLlamado);
            Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, auth.Esquema);
        }
    }
}

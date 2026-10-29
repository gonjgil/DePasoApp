using System.Security.Claims;
using DePaso.Web.Services;
using DePaso.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DePaso.Web.Controllers;

public class CuentaController : Controller
{
    private readonly UsuariosApiClient _usuariosApi;

    public CuentaController(UsuariosApiClient usuariosApi)
    {
        _usuariosApi = usuariosApi;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Ingresar()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new IngresarViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingresar(IngresarViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = await _usuariosApi.IngresarAsync(
            modelo.Email, 
            modelo.Contrasena);
        
        if (usuario == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Email o contraseña incorrectos.");
           
            return View(modelo);
        }

        await IniciarSesion(usuario);

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Registrarse()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegistrarseViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrarse(
        RegistrarseViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await _usuariosApi.RegistrarseAsync(
            modelo.Nombre, 
            modelo.Apellido, 
            modelo.Email, 
            modelo.Contrasena);

        if (resultado.Error is not null)
        {
            ModelState.AddModelError(
                nameof(modelo.Email),
                resultado.Error);

            return View(modelo);
        }

        await IniciarSesion(resultado.Usuario!);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Ingresar));
    }

    private async Task IniciarSesion(UsuarioApiResponse usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, 
            usuario.Id.ToString()),
            
            new(ClaimTypes.Name,
            $"{usuario.Nombre} {usuario.Apellido}"),
            
            new(ClaimTypes.Email, 
            usuario.Email)
        };

        var identidad = new ClaimsIdentity(
            claims, 
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad));
    }
}

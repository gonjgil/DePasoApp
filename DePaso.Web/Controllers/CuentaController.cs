using System.Security.Claims;
using DePaso.Entidades;
using DePaso.Logica;
using DePaso.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DePaso.Web.Controllers;

public class CuentaController : Controller
{
    private readonly UsuarioServicio _usuarios;

    public CuentaController(UsuarioServicio usuarios)
    {
        _usuarios = usuarios;
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

        var usuario = await _usuarios.Ingresar(modelo.Email, modelo.Contrasena);
        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
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
    public async Task<IActionResult> Registrarse(RegistrarseViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var (usuario, error) = await _usuarios.Registrar(modelo.Nombre, modelo.Apellido, modelo.Email, modelo.Contrasena);
        if (error != null)
        {
            ModelState.AddModelError(nameof(modelo.Email), error);
            return View(modelo);
        }

        await IniciarSesion(usuario!);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Ingresar));
    }

    private async Task IniciarSesion(Usuarios usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}"),
            new(ClaimTypes.Email, usuario.Email)
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad));
    }
}

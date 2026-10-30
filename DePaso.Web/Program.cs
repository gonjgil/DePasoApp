using DePaso.Entidades;
using DePaso.Logica;
using DePaso.Logica.helpers;
using DePaso.Logica.servicios;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DePasoAppContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Conexion"))
);

builder.Services.AddScoped<UsuarioServicio>();

builder.Services.AddSingleton<IEstimadorDesvio, EstimadorDesvioHaversine>();
builder.Services.AddSingleton<IOptimizadorCombinaciones, OptimizadorCombinaciones>();
builder.Services.AddScoped<IDashboardServicio, DashboardServicio>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Ingresar";
        options.AccessDeniedPath = "/Cuenta/Ingresar";
    });

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

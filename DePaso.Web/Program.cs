using DePaso.Entidades;
using DePaso.Logica;
using DePaso.Logica.helpers;
using DePaso.Logica.servicios;
using DePaso.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<UsuariosApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiUrl"]
            ?? throw new InvalidOperationException("No se configuró ApiUrl.")
    );
});

builder.Services.AddHttpClient<EnviosApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiUrl"]
            ?? throw new InvalidOperationException("No se configuró ApiUrl.")
    );
});

builder.Services.AddHttpClient<ViajesApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiUrl"]
            ?? throw new InvalidOperationException("No se configuró ApiUrl.")
    );
});

builder.Services.AddHttpClient<OportunidadesApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiUrl"]
            ?? throw new InvalidOperationException("No se configuró ApiUrl.")
    );
});

builder.Services.AddHttpClient<TransportesApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiUrl"]
            ?? throw new InvalidOperationException("No se configuró ApiUrl.")
    );
});

builder.Services.AddHttpClient<HistorialApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiUrl"]
            ?? throw new InvalidOperationException("No se configuró ApiUrl.")
    );
});

builder.Services.AddSingleton<IEstimadorDesvio, EstimadorDesvioHaversine>();
builder.Services.AddSingleton<IOptimizadorCombinaciones, OptimizadorCombinaciones>();
builder.Services.AddScoped<IDashboardServicio, DashboardServicio>();

builder.Services.AddDbContext<DePasoAppContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Conexion"))
);

builder
    .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Ingresar";
        options.AccessDeniedPath = "/Cuenta/Ingresar";
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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

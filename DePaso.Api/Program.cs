using DePaso.Api.Config;
using DePaso.Api.Services;
using DePaso.Entidades;
using DePaso.Logica;
using DePaso.Logica.servicios;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DePasoAppContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Conexion"))
);

builder.Services.AddScoped<UsuarioServicio>();
builder.Services.AddScoped<EnvioServicio>();
builder.Services.AddScoped<IHistorialServicio, HistorialServicio>();

builder.Services.AddControllers();

builder.Services.AgregarModuloViajes(builder.Configuration);

builder.Services.AddHttpClient<GeoapifyClient>(http =>
{
    http.BaseAddress = new Uri("https://api.geoapify.com/");
    http.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

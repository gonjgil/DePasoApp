using System.Reflection;
using DePaso.Api;
using DePaso.Api.Config;
using DePaso.Api.Services;
using DePaso.Entidades;
using DePaso.Logica;
using DePaso.Logica.servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DePasoAppContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Conexion"))
);

builder.Services.AddScoped<UsuarioServicio>();
builder.Services.AddScoped<EnvioServicio>();
builder.Services.AddScoped<IHistorialServicio, HistorialServicio>();
builder.Services.AddScoped<IDashboardServicio, DashboardServicio>();

builder.Services.AddControllers();

builder.Services.AgregarModuloViajes(builder.Configuration);

builder.Services.AddHttpClient<GeoapifyClient>(http =>
{
    http.BaseAddress = new Uri("https://api.geoapify.com/");
    http.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddOpenApi(opciones =>
{
    opciones.AddSchemaTransformer((esquema, _, _) =>
    {
        if (esquema.Type is JsonSchemaType tipo
            && tipo.HasFlag(JsonSchemaType.Integer)
            && tipo.HasFlag(JsonSchemaType.String))
        {
            esquema.Type = JsonSchemaType.Integer;
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(opciones =>
    {
        opciones.SwaggerEndpoint("/openapi/v1.json", "DePaso");
        opciones.RoutePrefix = "swagger";
    });
}

if (Assembly.GetEntryAssembly()?.GetName().Name != "ef")
{
    using var alcance = app.Services.CreateScope();
    var db = alcance.ServiceProvider.GetRequiredService<DePasoAppContext>();
    var usuarios = alcance.ServiceProvider.GetRequiredService<UsuarioServicio>();
    await DatosIniciales.CargarSiEstaVaciaAsync(db, usuarios);
}

app.UseWhen(
    contexto => !contexto.Request.Path.StartsWithSegments("/openapi")
        && !contexto.Request.Path.StartsWithSegments("/swagger"),
    rama => rama.UseHttpsRedirection());

app.UseAuthorization();

app.MapControllers();

app.Run();

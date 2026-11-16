using DePaso.Entidades;
using DePaso.Logica.dtos;
using DePaso.Logica.enums;
using DePaso.Logica.helpers;
using DePaso.Logica.servicios;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Tests.viajes.helper;

internal sealed class ViajesTestDbContext : DePasoAppContext
{
    public ViajesTestDbContext(DbContextOptions<DePasoAppContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
}

internal sealed class RutasFalsas : IServicioRutas
{
    private readonly ServicioRutasMock _mock = new();

    public HashSet<string> CallesNoUbicables { get; } = new(StringComparer.OrdinalIgnoreCase);
    public decimal KmPorTramo { get; set; } = 10m;
    public decimal? DistanciaFija { get; set; }
    public int LlamadasGeocodificar { get; private set; }
    public int LlamadasRuta { get; private set; }
    public List<IReadOnlyList<Coordenada>> Rutas { get; } = new();

    public Task<Coordenada?> GeocodificarAsync(DireccionDatos direccion, CancellationToken ct = default)
    {
        LlamadasGeocodificar++;
        if (CallesNoUbicables.Contains(direccion.Calle.Trim()))
        {
            return Task.FromResult<Coordenada?>(null);
        }

        return _mock.GeocodificarAsync(direccion, ct);
    }

    public Task<RutaCalculada> CalcularRutaAsync(IReadOnlyList<Coordenada> puntos, CancellationToken ct = default)
    {
        LlamadasRuta++;
        Rutas.Add(puntos.ToList());
        var km = DistanciaFija ?? KmPorTramo * (puntos.Count - 1);
        return Task.FromResult(new RutaCalculada(km, (int)(km * 1.5m), Estimada: true));
    }
}

internal sealed class EscenarioViajes
{
    private readonly string _nombre = Guid.NewGuid().ToString();

    public RutasFalsas Rutas { get; } = new();

    public DePasoAppContext Contexto() =>
        new ViajesTestDbContext(
            new DbContextOptionsBuilder<DePasoAppContext>().UseInMemoryDatabase(_nombre).Options);

    public ViajeServicio Servicio() => new(Contexto(), Rutas);

    public void Usuario(int id)
    {
        using var ctx = Contexto();
        ctx.Usuarios.Add(new Usuarios
        {
            Id = id,
            Nombre = $"Usuario{id}",
            Apellido = "Test",
            Email = $"u{id}@test.com",
            PasswordHash = "x",
            CreadoEn = DateTime.Now,
        });
        ctx.SaveChanges();
    }

    public async Task<ViajeDetalleDto> ViajeCreado(ViajeDatos datos)
    {
        var r = await Servicio().CrearAsync(datos);
        Assert.True(r.Exito, r.Mensaje);
        return r.Valor!;
    }

    public void EnvioAsignado(
        int envioId,
        int viajeId,
        EstadoEnvio estado,
        decimal pesoKg = 2,
        decimal desvioKm = 1,
        DateOnly? fecha = null,
        int remitenteId = 99)
    {
        using var ctx = Contexto();
        ctx.Envios.Add(new Envios
        {
            Id = envioId,
            UsuarioId = remitenteId,
            OrigenCalle = "Origen envío",
            OrigenAltura = 100,
            OrigenLocalidad = "CABA",
            OrigenProvincia = "Buenos Aires",
            OrigenLat = -34.6m,
            OrigenLng = -58.4m,
            DestinoCalle = "Destino envío",
            DestinoAltura = 200,
            DestinoLocalidad = "CABA",
            DestinoProvincia = "Buenos Aires",
            DestinoLat = -34.7m,
            DestinoLng = -58.4m,
            FechaLimite = new DateOnly(2026, 12, 31),
            PesoKg = pesoKg,
            Tamanio = 1,
            Descripcion = $"Paquete {envioId}",
            Monto = 3000,
            Estado = (byte)estado,
            CreadoEn = DateTime.Now,
        });
        ctx.Asignaciones.Add(new Asignaciones
        {
            EnvioId = envioId,
            ViajeId = viajeId,
            FechaViaje = fecha ?? new DateOnly(2026, 11, 20),
            DesvioKm = desvioKm,
            TiempoExtraMin = 5,
            Ganancia = 3000,
            AsignadoEn = DateTime.Now,
        });
        ctx.SaveChanges();
    }
}

internal static class Datos
{
    public static DireccionDatos Dir(string calle, int altura = 1000, string localidad = "CABA", decimal? lat = null, decimal? lng = null) =>
        new(calle, altura, null, null, localidad, "Buenos Aires", lat, lng);

    public static ParadaDatos Parada(string calle, int? orden = null, int altura = 500) => new(Dir(calle, altura), orden);

    public static ViajeDatos Viaje(
        int usuarioId = 1,
        DireccionDatos? origen = null,
        DireccionDatos? destino = null,
        byte[]? dias = null,
        TimeOnly? salida = null,
        TimeOnly? llegada = null,
        int maxPaquetes = 3,
        decimal pesoMax = 10,
        decimal desvioMax = 5,
        params ParadaDatos[] paradas) =>
        new(
            usuarioId,
            origen ?? Dir("Av. Cabildo", 1000),
            destino ?? Dir("Av. Hipólito Yrigoyen", 8000, "Lomas de Zamora"),
            dias ?? new byte[] { 1, 3, 5 },
            salida ?? new TimeOnly(8, 0),
            llegada ?? new TimeOnly(8, 55),
            maxPaquetes,
            pesoMax,
            desvioMax,
            paradas);
}
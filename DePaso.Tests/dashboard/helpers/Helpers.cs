using DePaso.Entidades;
using DePaso.Logica.enums;
using DePaso.Logica.helpers;
using Microsoft.EntityFrameworkCore;


namespace DePaso.Tests.dashboard.helpers
{
    internal sealed class TestDbContext : DePasoAppContext
    {
        public TestDbContext(DbContextOptions<DePasoAppContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }
    }

    internal static class Db
    {
        public static DePasoAppContext Nuevo(string? nombre = null)
        {
            var opciones = new DbContextOptionsBuilder<DePasoAppContext>()
                .UseInMemoryDatabase(nombre ?? Guid.NewGuid().ToString())
                .Options;
            return new TestDbContext(opciones);
        }
    }

    /// <summary>Reloj fijo: martes 27/10/2026 10:00 (hora local = UTC).</summary>
    internal sealed class RelojFijo : TimeProvider
    {
        private readonly DateTimeOffset _ahora;

        public RelojFijo(DateTime? ahora = null)
        {
            _ahora = new DateTimeOffset(ahora ?? new DateTime(2026, 10, 27, 10, 0, 0), TimeSpan.Zero);
        }

        public override DateTimeOffset GetUtcNow() => _ahora;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    /// <summary>Estimador de prueba: el desvío de un envío es su OrigenLat (1.5 => 1,5 km). Registra las rutas recibidas.</summary>
    internal sealed class EstimadorFijo : IEstimadorDesvio
    {
        public List<IReadOnlyList<Coordenada>> Rutas { get; } = new();

        public double DistanciaKm(Coordenada a, Coordenada b) => 0;

        public double CalcularDesvioKm(IReadOnlyList<Coordenada> rutaBase, Coordenada origenEnvio, Coordenada destinoEnvio)
        {
            Rutas.Add(rutaBase);
            return origenEnvio.Lat;
        }
    }

    internal static class Sembrar
    {
        public static readonly DateOnly Hoy = new(2026, 10, 27);
        public static readonly DateOnly FechaLejana = new(2026, 12, 31);

        public static Usuarios Usuario(DePasoAppContext ctx, int id, string nombre = "Juan")
        {
            var u = new Usuarios
            {
                Id = id,
                Nombre = nombre,
                Apellido = "Pérez",
                Email = $"u{id}@mail.com",
                PasswordHash = "x",
                CreadoEn = DateTime.Now
            };
            ctx.Usuarios.Add(u);
            ctx.SaveChanges();
            return u;
        }

        public static Viajes Viaje(
            DePasoAppContext ctx, int id, int usuarioId, byte[] dias,
            int salidaHora = 8, int maxPaquetes = 3, decimal pesoMax = 10, decimal desvioMax = 5,
            bool? activo = true, string origen = "Belgrano", string destino = "Lomas de Zamora")
        {
            var v = new Viajes
            {
                Id = id,
                UsuarioId = usuarioId,
                OrigenDir = origen,
                OrigenLat = -34.50m,
                OrigenLng = -58.40m,
                DestinoDir = destino,
                DestinoLat = -34.70m,
                DestinoLng = -58.40m,
                HoraSalida = new TimeOnly(salidaHora, 0),
                HoraLlegada = new TimeOnly(salidaHora, 55),
                MaxPaquetes = maxPaquetes,
                PesoMaxKg = pesoMax,
                DesvioMaxKm = desvioMax,
                DistanciaKm = 22,
                DuracionMin = 55,
                Activo = activo
            };
            foreach (var d in dias)
            {
                v.ViajeDia.Add(new ViajeDia { ViajeId = id, DiaSemana = d });
            }

            ctx.Viajes.Add(v);
            ctx.SaveChanges();
            return v;
        }

        public static Paradas Parada(DePasoAppContext ctx, int id, int viajeId, int orden, decimal lat, decimal lng)
        {
            var p = new Paradas { Id = id, ViajeId = viajeId, Orden = orden, Direccion = $"Parada {orden}", Lat = lat, Lng = lng };
            ctx.Paradas.Add(p);
            ctx.SaveChanges();
            return p;
        }

        /// <param name="desvioKm">Con <see cref="EstimadorFijo"/> el desvío sale de OrigenLat.</param>
        public static Envios Envio(
            DePasoAppContext ctx, int id, int usuarioId, decimal monto, decimal pesoKg = 1, decimal desvioKm = 0,
            EstadoEnvio estado = EstadoEnvio.BuscandoConductor, DateOnly? fechaLimite = null,
            string origen = "Palermo", string destino = "Avellaneda", string descripcion = "Paquete")
        {
            var e = new Envios
            {
                Id = id,
                UsuarioId = usuarioId,
                OrigenDir = origen,
                OrigenLat = desvioKm,
                OrigenLng = -58.40m,
                DestinoDir = destino,
                DestinoLat = -34.60m,
                DestinoLng = -58.40m,
                FechaLimite = fechaLimite ?? FechaLejana,
                PesoKg = pesoKg,
                Tamanio = 1,
                Descripcion = descripcion,
                Monto = monto,
                Estado = (byte)estado,
                CreadoEn = DateTime.Now
            };
            ctx.Envios.Add(e);
            ctx.SaveChanges();
            return e;
        }

        public static Asignaciones Asignar(DePasoAppContext ctx, int id, int envioId, int viajeId, DateOnly fecha, decimal desvioKm = 0)
        {
            var a = new Asignaciones
            {
                Id = id,
                EnvioId = envioId,
                ViajeId = viajeId,
                FechaViaje = fecha,
                DesvioKm = desvioKm,
                TiempoExtraMin = 5,
                Ganancia = 0,
                AsignadoEn = DateTime.Now
            };
            ctx.Asignaciones.Add(a);
            ctx.SaveChanges();
            return a;
        }
    }

}

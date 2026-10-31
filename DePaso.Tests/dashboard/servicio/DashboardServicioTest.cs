using DePaso.Entidades;
using DePaso.Logica.enums;
using DePaso.Logica.helpers;
using DePaso.Logica.servicios;
using DePaso.Tests.dashboard.helpers;

namespace DePaso.Tests.dashboard.servicio
{
    public class DashboardServicioTests
    {
        private static readonly byte[] LunMieVie = { 1, 3, 5 };
        private static readonly DateOnly Mie28 = new(2026, 10, 28);
        private static readonly DateOnly Vie30 = new(2026, 10, 30);

        private static (DashboardServicio Servicio, DePasoAppContext Ctx, EstimadorFijo Estimador) Crear()
        {
            var ctx = Db.Nuevo();
            var estimador = new EstimadorFijo();
            return (new DashboardServicio(ctx, estimador, new OptimizadorCombinaciones(), new RelojFijo()), ctx, estimador);
        }

        private static void SembrarEjemploEnunciado(DePasoAppContext ctx, int usuarioId)
        {
            Sembrar.Envio(ctx, 1, usuarioId, 4000, pesoKg: 2, desvioKm: 1m);   
            Sembrar.Envio(ctx, 2, usuarioId, 5500, pesoKg: 6, desvioKm: 2m);   
            Sembrar.Envio(ctx, 3, usuarioId, 3500, pesoKg: 4, desvioKm: 1m);   
            Sembrar.Envio(ctx, 4, usuarioId, 7000, pesoKg: 8, desvioKm: 3m);   
            Sembrar.Envio(ctx, 5, usuarioId, 2500, pesoKg: 1, desvioKm: 0.5m); 
        }

        // ---------- Usuario ----------

        [Fact]
        public async Task UsuarioInexistente_DevuelveNull()
        {
            var (servicio, _, _) = Crear();

            Assert.Null(await servicio.ObtenerResumenAsync(99));
        }

        [Fact]
        public async Task SinRelojExplicito_UsaElDelSistema()
        {
            var ctx = Db.Nuevo();
            Sembrar.Usuario(ctx, 1, "Ana");
            var servicio = new DashboardServicio(ctx, new EstimadorFijo(), new OptimizadorCombinaciones());

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal("Ana", r!.NombreUsuario);
        }

        [Fact]
        public async Task SinViajes_MuestraSoloSusEnviosPendientes()
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Envio(ctx, 1, 1, 4000, descripcion: "Zapatillas");

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal("Juan", r!.NombreUsuario);
            Assert.Equal(0, r.ProximosViajes);
            Assert.Empty(r.ListaProximosViajes);
            Assert.Equal(1, r.EnviosPendientes);
            Assert.Equal(0, r.Oportunidades);
            Assert.Equal(0m, r.PotencialMes);
        }

        // ---------- Próximos viajes ----------

        [Fact]
        public async Task ViajesInactivosOAjenosNoCuentan()
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Viaje(ctx, 1, 1, LunMieVie, activo: false);
            Sembrar.Viaje(ctx, 2, 1, LunMieVie, activo: null);
            Sembrar.Viaje(ctx, 3, 2, LunMieVie, activo: true);

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(0, r!.ProximosViajes);
            Assert.Empty(r.ListaProximosViajes);
        }

        [Fact]
        public async Task ViajeSinDiasNoTieneProximaSalida()
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Viaje(ctx, 1, 1, Array.Empty<byte>());

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(0, r!.ProximosViajes);
        }

        [Theory]
        [InlineData(18, 2026, 10, 27)] // sale hoy a las 18: todavía no pasó
        [InlineData(10, 2026, 11, 3)]  // sale a las 10:00 y ya son las 10:00: pasó
        [InlineData(8, 2026, 11, 3)]   // salió a las 8: la próxima es el martes siguiente
        public async Task ProximaSalida_DeHoySoloSiTodaviaNoPaso(int hora, int anio, int mes, int dia)
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Viaje(ctx, 1, 1, new byte[] { 2 }, salidaHora: hora);

            var r = await servicio.ObtenerResumenAsync(1);

            var viaje = Assert.Single(r!.ListaProximosViajes);
            Assert.Equal(new DateOnly(anio, mes, dia), viaje.ProximaFecha);
            Assert.Equal("Mar", viaje.DiasTexto);
            Assert.Equal("Belgrano", viaje.Origen);
            Assert.Equal("Lomas de Zamora", viaje.Destino);
            Assert.Equal(new TimeOnly(hora, 0), viaje.HoraSalida);
            Assert.Equal(new TimeOnly(hora, 55), viaje.HoraLlegada);
        }

        [Fact]
        public async Task ProximosViajes_OrdenadosPorProximaSalida()
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Viaje(ctx, 1, 1, new byte[] { 5 });
            Sembrar.Viaje(ctx, 2, 1, new byte[] { 3 });
            Sembrar.Viaje(ctx, 3, 1, new byte[] { 2 }, salidaHora: 18);

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(new[] { 3, 2, 1 }, r!.ListaProximosViajes.Select(v => v.Id));
            Assert.Equal(3, r.ProximosViajes);
        }

        [Fact]
        public async Task Listas_SeLimitanACincoPeroLosContadoresCuentanTodo()
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            for (var i = 1; i <= 7; i++)
            {
                Sembrar.Viaje(ctx, i, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7 });
                Sembrar.Envio(ctx, i, 1, 1000 * i);
            }

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(7, r!.ProximosViajes);
            Assert.Equal(DashboardServicio.MAX_ITEMS_LISTA, r.ListaProximosViajes.Count);
            Assert.Equal(7, r.EnviosPendientes);
            Assert.Equal(DashboardServicio.MAX_ITEMS_LISTA, r.ListaEnviosPendientes.Count);
        }

        // ---------- Envíos pendientes ----------

        [Fact]
        public async Task EnviosPendientes_SoloPropiosYSoloBuscandoOAsignado_OrdenadosPorFechaLimite()
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Envio(ctx, 1, 1, 4000, estado: EstadoEnvio.BuscandoConductor, fechaLimite: new DateOnly(2026, 11, 10));
            Sembrar.Envio(ctx, 2, 1, 3500, estado: EstadoEnvio.ConductorAsignado, fechaLimite: new DateOnly(2026, 11, 1));
            Sembrar.Envio(ctx, 3, 1, 100, estado: EstadoEnvio.Retirado);
            Sembrar.Envio(ctx, 4, 1, 100, estado: EstadoEnvio.EnCamino);
            Sembrar.Envio(ctx, 5, 1, 100, estado: EstadoEnvio.Entregado);
            Sembrar.Envio(ctx, 6, 1, 100, estado: EstadoEnvio.Cancelado);
            Sembrar.Envio(ctx, 7, 2, 100, estado: EstadoEnvio.BuscandoConductor);

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(2, r!.EnviosPendientes);
            Assert.Equal(new[] { 2, 1 }, r.ListaEnviosPendientes.Select(e => e.Id));
            Assert.Equal(new[] { "Conductor asignado", "Buscando conductor" }, r.ListaEnviosPendientes.Select(e => e.EstadoTexto));
            Assert.Equal(EstadoEnvio.ConductorAsignado, r.ListaEnviosPendientes[0].Estado);
            Assert.Equal(3500m, r.ListaEnviosPendientes[0].Monto);
            Assert.Equal("Palermo", r.ListaEnviosPendientes[0].Origen);
            Assert.Equal("Avellaneda", r.ListaEnviosPendientes[0].Destino);
            Assert.Equal("Paquete", r.ListaEnviosPendientes[0].Descripcion);
            Assert.Equal(new DateOnly(2026, 11, 1), r.ListaEnviosPendientes[0].FechaLimite);
        }

        // ---------- Oportunidades ----------

        [Fact]
        public async Task ViajeActivoSinEnviosDisponibles_NoTieneOportunidadesNiPotencial()
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Viaje(ctx, 1, 1, LunMieVie);

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(1, r!.ProximosViajes);
            Assert.Equal(0, r.Oportunidades);
            Assert.Equal(0m, r.PotencialMes);
        }

        [Fact]
        public async Task Oportunidades_ExcluyePropiosCanceladosAsignadosVencidosPesadosYLejanos()
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Usuario(ctx, 3);
            Sembrar.Viaje(ctx, 1, 1, LunMieVie); 
            Sembrar.Viaje(ctx, 50, 3, new byte[] { 1 });

            Sembrar.Envio(ctx, 10, 2, 1000); 
            Sembrar.Envio(ctx, 11, 1, 1000); 
            Sembrar.Envio(ctx, 12, 2, 1000, estado: EstadoEnvio.Cancelado); 
            Sembrar.Envio(ctx, 13, 2, 1000, estado: EstadoEnvio.ConductorAsignado);   
            Sembrar.Envio(ctx, 14, 2, 1000);  
            Sembrar.Asignar(ctx, 1, 14, 50, new DateOnly(2026, 11, 2));
            Sembrar.Envio(ctx, 15, 2, 1000, fechaLimite: new DateOnly(2026, 10, 26));  
            Sembrar.Envio(ctx, 16, 2, 1000, pesoKg: 11);    
            Sembrar.Envio(ctx, 17, 2, 1000, desvioKm: 6m);        

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(1, r!.Oportunidades);
            Assert.Equal(1000m, r.PotencialMes);
        }

        [Theory]
        [InlineData(27, 0)] // vence hoy y la próxima salida es mañana
        [InlineData(28, 1)] // vence el día de la salida: todavía sirve
        public async Task Oportunidades_RespetanLaFechaLimiteDelEnvio(int diaLimite, int esperadas)
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Viaje(ctx, 1, 1, LunMieVie);
            Sembrar.Envio(ctx, 10, 2, 1000, fechaLimite: new DateOnly(2026, 10, diaLimite));

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(esperadas, r!.Oportunidades);
        }

        [Theory]
        [InlineData(1, 1, 0, 0)]    // 1 paquete máx. y ya hay uno asignado en cada salida
        [InlineData(3, 9, 0, 0)]    // quedan 1 kg y el envío pesa 2
        [InlineData(3, 1, 4.5, 0)]  // quedan 0,5 km de desvío y el envío necesita 1
        [InlineData(3, 1, 0, 1)]    // entra
        public async Task Oportunidades_DescuentanLaCapacidadYaAsignada(int maxPaquetes, double pesoAsignado, double desvioAsignado, int esperadas)
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Usuario(ctx, 3);
            Sembrar.Viaje(ctx, 1, 1, LunMieVie, maxPaquetes: maxPaquetes, pesoMax: 10, desvioMax: 5);
            Sembrar.Envio(ctx, 10, 2, 1000, pesoKg: 2, desvioKm: 1m, fechaLimite: Vie30);

            // Ya hay un paquete comprometido en las dos salidas a las que llega el envío (28 y 30).
            Sembrar.Envio(ctx, 20, 3, 500, pesoKg: (decimal)pesoAsignado, estado: EstadoEnvio.ConductorAsignado);
            Sembrar.Envio(ctx, 21, 3, 500, pesoKg: (decimal)pesoAsignado, estado: EstadoEnvio.ConductorAsignado);
            Sembrar.Asignar(ctx, 1, 20, 1, Mie28, (decimal)desvioAsignado);
            Sembrar.Asignar(ctx, 2, 21, 1, Vie30, (decimal)desvioAsignado);

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(esperadas, r!.Oportunidades);
        }

        [Fact]
        public async Task Desvio_SeCalculaUnaVezPorViajeYEnvio()
        {
            var (servicio, ctx, estimador) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Viaje(ctx, 1, 1, LunMieVie);
            Sembrar.Envio(ctx, 10, 2, 1000);
            Sembrar.Envio(ctx, 11, 2, 1000);

            await servicio.ObtenerResumenAsync(1);

            // Hay 13 salidas en la ventana, pero solo 2 pares (viaje, envío).
            Assert.Equal(2, estimador.Rutas.Count);
        }

        [Fact]
        public async Task LaRutaBaseIncluyeLasParadasEnOrden()
        {
            var (servicio, ctx, estimador) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Viaje(ctx, 1, 1, LunMieVie);
            Sembrar.Parada(ctx, 1, 1, orden: 2, lat: -34.62m, lng: -58.40m);
            Sembrar.Parada(ctx, 2, 1, orden: 1, lat: -34.55m, lng: -58.40m);
            Sembrar.Envio(ctx, 10, 2, 1000);

            await servicio.ObtenerResumenAsync(1);

            var lats = estimador.Rutas[0].Select(p => p.Lat).ToArray();
            var esperado = new[] { -34.50, -34.55, -34.62, -34.70 };
            Assert.Equal(esperado.Length, lats.Length);
            for (var i = 0; i < esperado.Length; i++)
            {
                Assert.Equal(esperado[i], lats[i], 6);
            }
        }

        // ---------- Potencial del mes ----------

        [Fact]
        public async Task Potencial_EjemploDelEnunciado_ReparteLosEnviosEntreLasSalidasDelMes()
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Viaje(ctx, 1, 1, LunMieVie, maxPaquetes: 3, pesoMax: 10, desvioMax: 5);
            SembrarEjemploEnunciado(ctx, 2);

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(5, r!.Oportunidades);
            // Miercoles 28: A+B+E = 12.000. Viernes 30: quedan C y D (juntos pesan 12 kg) => D = 7.000.
            Assert.Equal(19000m, r.PotencialMes);
        }

        [Fact]
        public async Task Potencial_NoRepiteUnEnvioEntreViajes()
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Viaje(ctx, 1, 1, LunMieVie, maxPaquetes: 3, pesoMax: 10, desvioMax: 5);
            Sembrar.Viaje(ctx, 2, 1, LunMieVie, maxPaquetes: 3, pesoMax: 10, desvioMax: 5);
            SembrarEjemploEnunciado(ctx, 2);

            var r = await servicio.ObtenerResumenAsync(1);

            // 28/10 viaje 1: 12.000 | 28/10 viaje 2: D 7.000 | 30/10 viaje 1: C 3.500 | 30/10 viaje 2: nada.
            Assert.Equal(22500m, r!.PotencialMes);
            // Nunca puede superar la suma de todos los envíos disponibles (cada uno se usa una vez).
            Assert.True(r.PotencialMes <= 4000m + 5500m + 3500m + 7000m + 2500m);
            Assert.Equal(2, r.ProximosViajes);
        }

        [Fact]
        public async Task Potencial_SoloConsideraLasSalidasQueQuedanEnElMesActual()
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Viaje(ctx, 1, 1, LunMieVie, maxPaquetes: 3, pesoMax: 10, desvioMax: 5);
            // Pesan 6 kg: entra uno solo por salida. En octubre quedan 2 salidas (28 y 30).
            Sembrar.Envio(ctx, 1, 2, 5000, pesoKg: 6);
            Sembrar.Envio(ctx, 2, 2, 4000, pesoKg: 6);
            Sembrar.Envio(ctx, 3, 2, 3000, pesoKg: 6);

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(3, r!.Oportunidades);
            Assert.Equal(9000m, r.PotencialMes);
        }

        [Theory]
        [InlineData(8, 0)]     // la salida de hoy ya pasó y no quedan más martes en octubre
        [InlineData(18, 1000)] // la salida de hoy todavía no pasó
        public async Task Potencial_NoCuentaLaSalidaDeHoyQueYaPaso(int hora, int esperado)
        {
            var (servicio, ctx, _) = Crear();
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Viaje(ctx, 1, 1, new byte[] { 2 }, salidaHora: hora);
            Sembrar.Envio(ctx, 1, 2, 1000);

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(1, r!.Oportunidades);
            Assert.Equal((decimal)esperado, r.PotencialMes);
        }

        [Fact]
        public async Task ConEstimadorReal_ElEnvioCercanoEsOportunidadYElLejanoNo()
        {
            var ctx = Db.Nuevo();
            var servicio = new DashboardServicio(ctx, new EstimadorDesvioHaversine(), new OptimizadorCombinaciones(), new RelojFijo());
            Sembrar.Usuario(ctx, 1);
            Sembrar.Usuario(ctx, 2);
            Sembrar.Viaje(ctx, 1, 1, LunMieVie, desvioMax: 5);
            AgregarEnvio(ctx, 10, 2, 2000, -34.55m, -58.40m, -34.65m, -58.40m);
            AgregarEnvio(ctx, 11, 2, 9000, -34.55m, -57.90m, -34.65m, -57.90m);

            var r = await servicio.ObtenerResumenAsync(1);

            Assert.Equal(1, r!.Oportunidades);
            Assert.Equal(2000m, r.PotencialMes);
        }

        private static void AgregarEnvio(DePasoAppContext ctx, int id, int usuarioId, decimal monto,
            decimal origenLat, decimal origenLng, decimal destinoLat, decimal destinoLng)
        {
            ctx.Envios.Add(new Envios
            {
                Id = id,
                UsuarioId = usuarioId,
                OrigenDir = "O",
                OrigenLat = origenLat,
                OrigenLng = origenLng,
                DestinoDir = "D",
                DestinoLat = destinoLat,
                DestinoLng = destinoLng,
                FechaLimite = Sembrar.FechaLejana,
                PesoKg = 1,
                Tamanio = 1,
                Descripcion = "Paquete",
                Monto = monto,
                Estado = (byte)EstadoEnvio.BuscandoConductor,
                CreadoEn = DateTime.Now
            });
            ctx.SaveChanges();
        }
    }
}

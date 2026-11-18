using DePaso.Entidades;
using DePaso.Logica;
using DePaso.Logica.enums;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Api;

public static class DatosIniciales
{
    public const string Clave = "Demo1234";
    public const string EmailConductor = "demo.conductor@depaso.test";
    public const string EmailCliente = "demo.cliente@depaso.test";

    public static async Task CargarSiEstaVaciaAsync(DePasoAppContext db, UsuarioServicio usuarios)
    {
        if (await db.Usuarios.AnyAsync())
        {
            return;
        }

        var conductor = (await usuarios.Registrar("Demo", "Conductor", EmailConductor, Clave)).Usuario!;
        var cliente = (await usuarios.Registrar("Demo", "Cliente", EmailCliente, Clave)).Usuario!;

        var viaje = new Viajes
        {
            UsuarioId = conductor.Id,
            OrigenCalle = "Cabildo",
            OrigenAltura = 2000,
            OrigenLocalidad = "Belgrano",
            OrigenProvincia = "CABA",
            OrigenLat = -34.563162m,
            OrigenLng = -58.455998m,
            DestinoCalle = "Hipolito Yrigoyen",
            DestinoAltura = 2000,
            DestinoLocalidad = "Lomas de Zamora",
            DestinoProvincia = "Buenos Aires",
            DestinoLat = -34.674917m,
            DestinoLng = -58.556856m,
            HoraSalida = new TimeOnly(8, 0),
            HoraLlegada = new TimeOnly(9, 30),
            MaxPaquetes = 4,
            PesoMaxKg = 30,
            DesvioMaxKm = 20,
            DistanciaKm = 24,
            DuracionMin = 40,
            Activo = true,
        };
        db.Viajes.Add(viaje);
        await db.SaveChangesAsync();

        db.ViajeDia.AddRange(
            new ViajeDia { ViajeId = viaje.Id, DiaSemana = 1 },
            new ViajeDia { ViajeId = viaje.Id, DiaSemana = 5 }
        );
        db.Paradas.Add(new Paradas
        {
            ViajeId = viaje.Id,
            Orden = 1,
            Direccion = "El Esporal 5400, Ciudad Evita",
            Calle = "El Esporal",
            Altura = 5400,
            Localidad = "Ciudad Evita",
            Provincia = "Buenos Aires",
            Lat = -34.725472m,
            Lng = -58.523220m,
        });

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var entregado = new Envios
        {
            UsuarioId = cliente.Id,
            OrigenCalle = "Cabildo",
            OrigenAltura = 2100,
            OrigenLocalidad = "Belgrano",
            OrigenProvincia = "CABA",
            OrigenLat = -34.564000m,
            OrigenLng = -58.457000m,
            DestinoCalle = "Hipolito Yrigoyen",
            DestinoAltura = 1800,
            DestinoLocalidad = "Lomas de Zamora",
            DestinoProvincia = "Buenos Aires",
            DestinoLat = -34.675500m,
            DestinoLng = -58.556200m,
            FechaLimite = hoy.AddMonths(-1),
            PesoKg = 2,
            Tamanio = 1,
            Descripcion = "Caja",
            Monto = 8000,
            Estado = (byte)EstadoEnvio.Entregado,
            CreadoEn = DateTime.Today.AddMonths(-2),
            RowVersion = 0,
        };
        var oportunidad = new Envios
        {
            UsuarioId = cliente.Id,
            OrigenCalle = "Cabildo",
            OrigenAltura = 2050,
            OrigenLocalidad = "Belgrano",
            OrigenProvincia = "CABA",
            OrigenLat = -34.564000m,
            OrigenLng = -58.457000m,
            DestinoCalle = "Hipolito Yrigoyen",
            DestinoAltura = 2100,
            DestinoLocalidad = "Lomas de Zamora",
            DestinoProvincia = "Buenos Aires",
            DestinoLat = -34.675500m,
            DestinoLng = -58.556200m,
            FechaLimite = hoy.AddMonths(1),
            PesoKg = 1.5m,
            Tamanio = 1,
            Descripcion = "Documentos",
            Monto = 6500,
            Estado = (byte)EstadoEnvio.BuscandoConductor,
            CreadoEn = DateTime.Today,
            RowVersion = 0,
        };
        db.Envios.AddRange(entregado, oportunidad);
        await db.SaveChangesAsync();

        db.Asignaciones.Add(new Asignaciones
        {
            EnvioId = entregado.Id,
            ViajeId = viaje.Id,
            FechaViaje = hoy.AddMonths(-1),
            DesvioKm = 0.5m,
            TiempoExtraMin = 10,
            Ganancia = 8000,
            AsignadoEn = DateTime.Today.AddMonths(-1),
        });
        await db.SaveChangesAsync();
    }
}

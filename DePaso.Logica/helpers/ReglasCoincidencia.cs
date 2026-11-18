using System.Globalization;
using DePaso.Entidades;
using DePaso.Logica.enums;

namespace DePaso.Logica.helpers;

/// <summary>
/// Reglas de coincidencia entre envíos y salidas de un viaje. Las comparten el dashboard
/// (oportunidades / potencial) y el servicio de coincidencias.
/// </summary>
public static class ReglasCoincidencia
{
    public const decimal UmbralMuyCompatibleKm = 1m;
    public const decimal UmbralCompatibleKm = 2m;

    public sealed record OcupacionSalida(int ViajeId, DateOnly Fecha, decimal PesoKg, decimal DesvioKm);

    public sealed record Evaluado(Envios Envio, decimal DesvioKm, IReadOnlyList<string> Motivos)
    {
        public bool Entra => Motivos.Count == 0;
    }

    /// <summary>Capacidad que queda en una salida después de las asignaciones ya confirmadas para esa fecha.</summary>
    public static LimitesViaje Restantes(Viajes viaje, DateOnly fecha, IEnumerable<OcupacionSalida> ocupacion)
    {
        var asignadas = ocupacion.Where(a => a.ViajeId == viaje.Id && a.Fecha == fecha).ToList();
        return new LimitesViaje(
            viaje.MaxPaquetes - asignadas.Count,
            viaje.PesoMaxKg - asignadas.Sum(a => a.PesoKg),
            viaje.DesvioMaxKm - asignadas.Sum(a => a.DesvioKm)
        );
    }

    public static List<Coordenada> ConstruirRuta(Viajes viaje)
    {
        var puntos = new List<Coordenada> { new((double)viaje.OrigenLat, (double)viaje.OrigenLng) };
        puntos.AddRange(
            viaje.Paradas.OrderBy(p => p.Orden).Select(p => new Coordenada((double)p.Lat, (double)p.Lng))
        );
        puntos.Add(new Coordenada((double)viaje.DestinoLat, (double)viaje.DestinoLng));
        return puntos;
    }

    public static Coordenada OrigenDe(Envios e) => new((double)e.OrigenLat, (double)e.OrigenLng);

    public static Coordenada DestinoDe(Envios e) => new((double)e.DestinoLat, (double)e.DestinoLng);

    public static decimal Desvio(IEstimadorDesvio estimador, IReadOnlyList<Coordenada> ruta, Envios envio) =>
        decimal.Round((decimal)estimador.CalcularDesvioKm(ruta, OrigenDe(envio), DestinoDe(envio)), 2);

    /// <summary>Evalúa cada envío contra los límites restantes de la salida y anota los motivos de rechazo.</summary>
    public static List<Evaluado> Evaluar(
        IEnumerable<Envios> candidatos,
        LimitesViaje limites,
        Func<Envios, decimal> desvioDe
    ) => candidatos.Select(e => Evaluar(e, limites, desvioDe(e))).ToList();

    public static Evaluado Evaluar(Envios envio, LimitesViaje limites, decimal desvioKm)
    {
        var motivos = new List<string>();
        if (limites.MaxPaquetes <= 0)
        {
            motivos.Add("No quedan paquetes en la salida.");
        }
        if (envio.PesoKg > limites.PesoMaxKg)
        {
            motivos.Add($"Supera el peso que queda en la salida ({Numero(limites.PesoMaxKg)} kg).");
        }
        if (desvioKm > limites.DesvioMaxKm)
        {
            motivos.Add($"Supera el desvío que queda en la salida ({Numero(limites.DesvioMaxKm)} km).");
        }
        return new Evaluado(envio, desvioKm, motivos);
    }

    public static NivelCoincidencia Nivel(decimal desvioKm) =>
        desvioKm <= UmbralMuyCompatibleKm ? NivelCoincidencia.MuyCompatible
        : desvioKm <= UmbralCompatibleKm ? NivelCoincidencia.Compatible
        : NivelCoincidencia.MuchoDesvio;

    /// <summary>
    /// Ruta base con el envío insertado en la posición de menor desvío. Usa el mismo criterio que
    /// <see cref="IEstimadorDesvio.CalcularDesvioKm"/>: retiro y entrega en el mismo tramo o en tramos
    /// distintos con el retiro antes que la entrega.
    /// </summary>
    public static List<Coordenada> RutaConEnvio(
        IEstimadorDesvio estimador,
        IReadOnlyList<Coordenada> rutaBase,
        Coordenada origen,
        Coordenada destino
    )
    {
        if (rutaBase.Count < 2)
        {
            throw new ArgumentException("La ruta base necesita al menos origen y destino.", nameof(rutaBase));
        }

        var segmentos = rutaBase.Count - 1;
        var mejor = double.MaxValue;
        var mejorI = 0;
        var mejorJ = 0;

        for (var i = 0; i < segmentos; i++)
        {
            var extraRetiro = Extra(estimador, rutaBase[i], rutaBase[i + 1], origen);

            var mismo = estimador.DistanciaKm(rutaBase[i], origen)
                        + estimador.DistanciaKm(origen, destino)
                        + estimador.DistanciaKm(destino, rutaBase[i + 1])
                        - estimador.DistanciaKm(rutaBase[i], rutaBase[i + 1]);
            if (mismo < mejor)
            {
                mejor = mismo;
                mejorI = i;
                mejorJ = i;
            }

            for (var j = i + 1; j < segmentos; j++)
            {
                var costo = extraRetiro + Extra(estimador, rutaBase[j], rutaBase[j + 1], destino);
                if (costo < mejor)
                {
                    mejor = costo;
                    mejorI = i;
                    mejorJ = j;
                }
            }
        }

        var ruta = new List<Coordenada>(rutaBase.Count + 2);
        for (var k = 0; k < rutaBase.Count; k++)
        {
            ruta.Add(rutaBase[k]);
            if (k == mejorI)
            {
                ruta.Add(origen);
            }
            if (k == mejorJ)
            {
                ruta.Add(destino);
            }
        }
        return ruta;
    }

    private static double Extra(IEstimadorDesvio estimador, Coordenada a, Coordenada b, Coordenada p) =>
        estimador.DistanciaKm(a, p) + estimador.DistanciaKm(p, b) - estimador.DistanciaKm(a, b);

    /// <summary>Una salida concreta de un viaje: viaje + fecha.</summary>
    public sealed record Ocurrencia(Viajes Viaje, DateOnly Fecha);

    /// <summary>
    /// Salidas de los viajes dentro de una ventana de días desde hoy. Excluye la salida de hoy si ya pasó.
    /// </summary>
    public static List<Ocurrencia> GenerarOcurrencias(
        IEnumerable<Viajes> viajes,
        DateOnly hoy,
        TimeOnly horaActual,
        int diasVentana
    )
    {
        var resultado = new List<Ocurrencia>();
        foreach (var viaje in viajes)
        {
            var dias = viaje.ViajeDia.Select(d => d.DiaSemana).ToHashSet();
            for (var i = 0; i < diasVentana; i++)
            {
                var fecha = hoy.AddDays(i);
                if (!dias.Contains(DiasSemana.Numero(fecha.DayOfWeek)))
                {
                    continue;
                }

                if (i == 0 && viaje.HoraSalida <= horaActual)
                {
                    continue;
                }

                resultado.Add(new Ocurrencia(viaje, fecha));
            }
        }

        return resultado
            .OrderBy(o => o.Fecha)
            .ThenBy(o => o.Viaje.HoraSalida)
            .ThenBy(o => o.Viaje.Id)
            .ToList();
    }

    private static string Numero(decimal valor) =>
        valor.ToString("0.##", CultureInfo.GetCultureInfo("es-AR"));
}

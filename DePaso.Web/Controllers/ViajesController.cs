using System.Globalization;
using DePaso.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DePaso.Web.Controllers;

public sealed class CulturaInvarianteAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}

[Authorize]
[CulturaInvariante]
public class ViajesController : Controller
{

    private const double VelocidadPromedioKmh = 40;

    private static readonly string[] CamposQueNoAfectanLaRuta =
    [
        "Dias",
        "HoraSalida",
        "HoraLlegada",
        "MaxPaquetes",
        "PesoMaxKg",
        "DesvioMaxKm",
    ];

    [HttpGet]
    public IActionResult Nuevo()
    {
        return View(new NuevoViajeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Nuevo(NuevoViajeViewModel modelo, string? accion)
    {
        modelo.Paradas = (modelo.Paradas ?? new()).Where(parada => !parada.EstaVacia()).ToList();
        modelo.Dias = (modelo.Dias ?? new()).Where(dia => dia is >= 1 and <= 7).Distinct().ToList();
        modelo.HoraSalida = LeerHora(Request.Form["HoraSalida"]);
        modelo.HoraLlegada = LeerHora(Request.Form["HoraLlegada"]);

        ModelState.Clear();
        TryValidateModel(modelo);

        if (accion == "calcular")
        {
            foreach (var campo in CamposQueNoAfectanLaRuta)
            {
                ModelState.Remove(campo);
            }

            ValidarCoordenadasParaRuta(modelo);
            if (ModelState.IsValid)
            {
                (modelo.DistanciaKm, modelo.DuracionMin) = EstimarRuta(modelo);
            }

            return View(modelo);
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        if (TieneCoordenadas(modelo.OrigenLat, modelo.OrigenLng)
            && TieneCoordenadas(modelo.DestinoLat, modelo.DestinoLng)
            && modelo.Paradas.All(parada => TieneCoordenadas(parada.Lat, parada.Lng)))
        {
            (modelo.DistanciaKm, modelo.DuracionMin) = EstimarRuta(modelo);
        }

        ViewData["Validado"] = true;
        return View(modelo);
    }

    private void ValidarCoordenadasParaRuta(NuevoViajeViewModel modelo)
    {
        if (!TieneCoordenadas(modelo.OrigenLat, modelo.OrigenLng))
        {
            ModelState.AddModelError(nameof(modelo.OrigenLat), "Completá las coordenadas de origen para calcular la ruta.");
        }

        if (!TieneCoordenadas(modelo.DestinoLat, modelo.DestinoLng))
        {
            ModelState.AddModelError(nameof(modelo.DestinoLat), "Completá las coordenadas de destino para calcular la ruta.");
        }

        for (var i = 0; i < modelo.Paradas.Count; i++)
        {
            var parada = modelo.Paradas[i];
            if (!TieneCoordenadas(parada.Lat, parada.Lng))
            {
                ModelState.AddModelError($"Paradas[{i}].Lat", "Completá las coordenadas de la parada para calcular la ruta.");
            }
        }
    }

    private static readonly string[] FormatosHora = ["HH:mm", "HH:mm:ss", "H:mm", "HH:mm:ss.FFFFFFF"];

    private static TimeOnly? LeerHora(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        return TimeOnly.TryParseExact(
            texto.Trim(),
            FormatosHora,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var hora
        )
            ? hora
            : null;
    }

    private static bool TieneCoordenadas(decimal? lat, decimal? lng) =>
        lat is not null && lng is not null && (lat != 0 || lng != 0);

    private static bool TieneCoordenadas(decimal lat, decimal lng) => lat != 0 || lng != 0;

    private static (decimal kilometros, int minutos) EstimarRuta(NuevoViajeViewModel modelo)
    {
        var puntos = new List<(double Lat, double Lng)>
        {
            ((double)modelo.OrigenLat.GetValueOrDefault(), (double)modelo.OrigenLng.GetValueOrDefault()),
        };

        foreach (var parada in modelo.Paradas)
        {
            puntos.Add(((double)parada.Lat, (double)parada.Lng));
        }

        puntos.Add(((double)modelo.DestinoLat.GetValueOrDefault(), (double)modelo.DestinoLng.GetValueOrDefault()));

        double total = 0;
        for (var i = 1; i < puntos.Count; i++)
        {
            total += DistanciaKm(puntos[i - 1], puntos[i]);
        }

        var kilometros = Math.Round((decimal)total, 1);
        var minutos = (int)Math.Round(total / VelocidadPromedioKmh * 60d);
        if (minutos < 1 && total > 0)
        {
            minutos = 1;
        }

        return (kilometros, minutos);
    }

    private static double DistanciaKm((double Lat, double Lng) desde, (double Lat, double Lng) hasta)
    {
        const double radioKm = 6371;
        var lat1 = GradosARadianes(desde.Lat);
        var lat2 = GradosARadianes(hasta.Lat);
        var dLat = GradosARadianes(hasta.Lat - desde.Lat);
        var dLng = GradosARadianes(hasta.Lng - desde.Lng);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return radioKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double GradosARadianes(double grados) => grados * Math.PI / 180;
}

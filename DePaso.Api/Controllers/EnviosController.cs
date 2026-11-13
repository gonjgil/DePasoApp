using System.Globalization;
using DePaso.Api.Models;
using DePaso.Api.Services;
using DePaso.Logica;
using Microsoft.AspNetCore.Mvc;

namespace DePaso.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnviosController : Controller
{
    private readonly EnvioServicio _envios;
    private readonly GeoapifyClient _geoapify;

    public EnviosController(EnvioServicio envios, GeoapifyClient geoapify)
    {
        _envios = envios ?? throw new ArgumentNullException(nameof(envios));
        _geoapify = geoapify ?? throw new ArgumentNullException(nameof(geoapify));
    }

    [HttpGet("mios")]
    public async Task<IActionResult> MisEnvios([FromQuery] int usuarioId)
    {
        var envios = await _envios.ObtenerPorUsuario(usuarioId);

        return Ok(
            envios.Select(e => new
            {
                e.Id,
                e.UsuarioId,
                e.OrigenCalle,
                e.OrigenAltura,
                e.OrigenPiso,
                e.OrigenDepartamento,
                e.OrigenLocalidad,
                e.OrigenProvincia,
                e.OrigenLat,
                e.OrigenLng,
                e.DestinoCalle,
                e.DestinoAltura,
                e.DestinoPiso,
                e.DestinoDepartamento,
                e.DestinoLocalidad,
                e.DestinoProvincia,
                e.DestinoLat,
                e.DestinoLng,
                e.FechaLimite,
                e.PesoKg,
                e.Tamanio,
                e.Descripcion,
                e.Monto,
                e.Estado,
                e.CreadoEn,
            })
        );
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id, [FromQuery] int usuarioId)
    {
        var envio = await _envios.ObtenerPorId(id);

        if (envio is null)
        {
            return NotFound(new { mensaje = "Envío no encontrado." });
        }

        return Ok(
            new
            {
                envio.Id,
                envio.UsuarioId,
                envio.OrigenCalle,
                envio.OrigenAltura,
                envio.OrigenPiso,
                envio.OrigenDepartamento,
                envio.OrigenLocalidad,
                envio.OrigenProvincia,
                envio.OrigenLat,
                envio.OrigenLng,
                envio.DestinoCalle,
                envio.DestinoAltura,
                envio.DestinoPiso,
                envio.DestinoDepartamento,
                envio.DestinoLocalidad,
                envio.DestinoProvincia,
                envio.DestinoLat,
                envio.DestinoLng,
                envio.FechaLimite,
                envio.PesoKg,
                envio.Tamanio,
                envio.Descripcion,
                envio.Monto,
                envio.Estado,
                envio.CreadoEn,
            }
        );
    }

    [HttpPost("crear")]
    public async Task<IActionResult> Crear([FromBody] EnvioAlta modelo, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var origen = modelo.Origen;
        var destino = modelo.Destino;
        var origenAltura = origen.Altura;
        var destinoAltura = destino.Altura;

        if (origenAltura <= 0 || destinoAltura <= 0)
        {
            return BadRequest(
                new { mensaje = "Ingresá una altura válida para ambas direcciones." }
            );
        }

        var resultadoOrigen = await BuscarDireccionAsync(origen, origenAltura, ct);

        if (
            resultadoOrigen?.Lat is not decimal origenLat
            || resultadoOrigen?.Lon is not decimal origenLng
        )
        {
            return BadRequest(new { mensaje = "No se pudo ubicar la dirección de origen." });
        }

        var resultadoDestino = await BuscarDireccionAsync(destino, destinoAltura, ct);

        if (
            resultadoDestino?.Lat is not decimal destinoLat
            || resultadoDestino?.Lon is not decimal destinoLng
        )
        {
            return BadRequest(new { mensaje = "No se pudo ubicar la dirección de destino." });
        }

        var envio = await _envios.Crear(
            modelo.UsuarioId,
            modelo.Origen.Calle,
            origenAltura,
            modelo.Origen.Piso,
            modelo.Origen.Departamento,
            modelo.Origen.Localidad,
            modelo.Origen.Provincia,
            origenLat,
            origenLng,
            modelo.Destino.Calle,
            destinoAltura,
            modelo.Destino.Piso,
            modelo.Destino.Departamento,
            modelo.Destino.Localidad,
            modelo.Destino.Provincia,
            destinoLat,
            destinoLng,
            modelo.FechaLimite,
            modelo.PesoKg,
            modelo.Tamanio,
            modelo.Descripcion,
            modelo.Monto
        );

        if (envio is null)
        {
            return NotFound(new { mensaje = "Usuario no encontrado." });
        }

        return Created(
            string.Empty,
            new
            {
                envio.Id,
                envio.UsuarioId,
                envio.OrigenCalle,
                envio.OrigenAltura,
                envio.OrigenPiso,
                envio.OrigenDepartamento,
                envio.OrigenLocalidad,
                envio.OrigenProvincia,
                envio.OrigenLat,
                envio.OrigenLng,
                envio.DestinoCalle,
                envio.DestinoAltura,
                envio.DestinoPiso,
                envio.DestinoDepartamento,
                envio.DestinoLocalidad,
                envio.DestinoProvincia,
                envio.DestinoLat,
                envio.DestinoLng,
                envio.FechaLimite,
                envio.PesoKg,
                envio.Tamanio,
                envio.Descripcion,
                envio.Monto,
                envio.Estado,
                envio.CreadoEn,
            }
        );
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(
        int id,
        [FromBody] EnvioAlta modelo,
        CancellationToken ct
    )
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var actual = await _envios.ObtenerPorId(id);
        if (actual is null)
        {
            return NotFound(new { mensaje = "Envío no encontrado." });
        }

        var (origen, destino) = (modelo.Origen, modelo.Destino);
        var (origenAltura, destinoAltura) = (origen.Altura, destino.Altura);

        if (origenAltura <= 0 || destinoAltura <= 0)
        {
            return BadRequest(
                new { mensaje = "Ingresá una altura válida para ambas direcciones." }
            );
        }

        var resultadoOrigen = await BuscarDireccionAsync(origen, origenAltura, ct);

        if (
            resultadoOrigen?.Lat is not decimal origenLat
            || resultadoOrigen?.Lon is not decimal origenLng
        )
        {
            return BadRequest(new { mensaje = "No se pudo ubicar la dirección de origen." });
        }

        var resultadoDestino = await BuscarDireccionAsync(destino, destinoAltura, ct);

        if (
            resultadoDestino?.Lat is not decimal destinoLat
            || resultadoDestino?.Lon is not decimal destinoLng
        )
        {
            return BadRequest(new { mensaje = "No se pudo ubicar la dirección de destino." });
        }

        var envio = await _envios.Editar(
            id,
            modelo.UsuarioId,
            modelo.Origen.Calle,
            origenAltura,
            modelo.Origen.Piso,
            modelo.Origen.Departamento,
            modelo.Origen.Localidad,
            modelo.Origen.Provincia,
            origenLat,
            origenLng,
            modelo.Destino.Calle,
            destinoAltura,
            modelo.Destino.Piso,
            modelo.Destino.Departamento,
            modelo.Destino.Localidad,
            modelo.Destino.Provincia,
            destinoLat,
            destinoLng,
            modelo.FechaLimite,
            modelo.PesoKg,
            modelo.Tamanio,
            modelo.Descripcion,
            modelo.Monto
        );

        if (envio is null)
        {
            return NotFound(new { mensaje = "Envío no encontrado." });
        }

        return Ok(
            new
            {
                envio.Id,
                envio.UsuarioId,
                envio.OrigenCalle,
                envio.OrigenAltura,
                envio.OrigenPiso,
                envio.OrigenDepartamento,
                envio.OrigenLocalidad,
                envio.OrigenProvincia,
                envio.OrigenLat,
                envio.OrigenLng,
                envio.DestinoCalle,
                envio.DestinoAltura,
                envio.DestinoPiso,
                envio.DestinoDepartamento,
                envio.DestinoLocalidad,
                envio.DestinoProvincia,
                envio.DestinoLat,
                envio.DestinoLng,
                envio.FechaLimite,
                envio.PesoKg,
                envio.Tamanio,
                envio.Descripcion,
                envio.Monto,
                envio.Estado,
                envio.CreadoEn,
            }
        );
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Borrar(int id, [FromQuery] int usuarioId)
    {
        var eliminado = await _envios.Borrar(id, usuarioId);

        if (!eliminado)
        {
            return NotFound(new { mensaje = "Envío no encontrado." });
        }

        return Ok(new { mensaje = "Envío eliminado." });
    }

    private async Task<DireccionGeoapify?> BuscarDireccionAsync(
        DireccionEnvioRegistro direccion,
        int altura,
        CancellationToken ct
    )
    {
        var texto =
            $"{direccion.Calle.Trim()} {altura}, "
            + $"{direccion.Localidad.Trim()}, {direccion.Provincia.Trim()}, Argentina";

        var resultados = await _geoapify.BuscarAsync(texto, ct);

        var coincidencias = resultados
            .Where(r =>
                r.Lat.HasValue
                && r.Lon.HasValue
                && int.TryParse(r.Housenumber, out var numero)
                && numero == altura
                && MismoNombre(r.Street, direccion.Calle)
                && MismoNombre(r.State, direccion.Provincia)
                && (
                    MismoNombre(r.City, direccion.Localidad)
                    || MismoNombre(r.Suburb, direccion.Localidad)
                    || MismoNombre(r.District, direccion.Localidad)
                )
            )
            .DistinctBy(r => (r.Lat, r.Lon))
            .Take(2)
            .ToArray();

        return coincidencias.Length == 1 ? coincidencias[0] : null;
    }

    private static bool MismoNombre(string? recibido, string ingresado) =>
        CultureInfo
            .GetCultureInfo("es-AR")
            .CompareInfo.Compare(
                recibido?.Trim(),
                ingresado.Trim(),
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace
            ) == 0;

    private static bool CambioDireccion(
        DireccionEnvioRegistro nueva,
        string calle,
        int altura,
        string localidad,
        string provincia
    ) =>
        nueva.Altura != altura
        || !MismoNombre(calle, nueva.Calle)
        || !MismoNombre(localidad, nueva.Localidad)
        || !MismoNombre(provincia, nueva.Provincia);
}

using DePaso.Entidades;
using DePaso.Logica.enums;
using Microsoft.EntityFrameworkCore;

namespace DePaso.Logica;

public class EnvioServicio
{
    private readonly DePasoAppContext _context;

    public EnvioServicio(DePasoAppContext context)
    {
        _context = context;
    }

    public async Task<Envios?> Crear(
        int usuarioId,
        string origenCalle,
        int origenAltura,
        string? origenPiso,
        string? origenDepartamento,
        string origenLocalidad,
        string origenProvincia,
        decimal origenLat,
        decimal origenLng,
        string destinoCalle,
        int destinoAltura,
        string? destinoPiso,
        string? destinoDepartamento,
        string destinoLocalidad,
        string destinoProvincia,
        decimal destinoLat,
        decimal destinoLng,
        DateOnly fechaLimite,
        decimal pesoKg,
        byte tamanio,
        string descripcion,
        decimal monto
    )
    {
        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario is null)
        {
            return null;
        }

        var envio = new Envios
        {
            UsuarioId = usuarioId,
            OrigenCalle = origenCalle,
            OrigenAltura = origenAltura,
            OrigenPiso = origenPiso,
            OrigenDepartamento = origenDepartamento,
            OrigenLocalidad = origenLocalidad,
            OrigenProvincia = origenProvincia,
            OrigenLat = origenLat,
            OrigenLng = origenLng,
            DestinoCalle = destinoCalle,
            DestinoAltura = destinoAltura,
            DestinoPiso = destinoPiso,
            DestinoDepartamento = destinoDepartamento,
            DestinoLocalidad = destinoLocalidad,
            DestinoProvincia = destinoProvincia,
            DestinoLat = destinoLat,
            DestinoLng = destinoLng,
            FechaLimite = fechaLimite,
            PesoKg = pesoKg,
            Tamanio = tamanio,
            Descripcion = descripcion.Trim(),
            Monto = monto,
            Estado = (byte)EstadoEnvio.BuscandoConductor,
            CreadoEn = DateTime.Now,
            RowVersion = 0,
        };

        _context.Envios.Add(envio);
        await _context.SaveChangesAsync();

        return envio;
    }

    public async Task<List<Envios>> ObtenerPorUsuario(int usuarioId)
    {
        return await _context
            .Envios.AsNoTracking()
            .Where(e => e.UsuarioId == usuarioId)
            .OrderByDescending(e => e.CreadoEn)
            .Take(20)
            .ToListAsync();
    }

    public async Task<Envios?> ObtenerPorId(int id)
    {
        return await _context.Envios.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Envios?> Editar(
        int id,
        int usuarioId,
        string origenCalle,
        int origenAltura,
        string? origenPiso,
        string? origenDepartamento,
        string origenLocalidad,
        string origenProvincia,
        decimal origenLat,
        decimal origenLng,
        string destinoCalle,
        int destinoAltura,
        string? destinoPiso,
        string? destinoDepartamento,
        string destinoLocalidad,
        string destinoProvincia,
        decimal destinoLat,
        decimal destinoLng,
        DateOnly fechaLimite,
        decimal pesoKg,
        byte tamanio,
        string descripcion,
        decimal monto
    )
    {
        var envio = await _context.Envios.FirstOrDefaultAsync(e =>
            e.Id == id && e.UsuarioId == usuarioId
        );
        if (envio is null)
        {
            return null;
        }

        envio.OrigenCalle = origenCalle;
        envio.OrigenAltura = origenAltura;
        envio.OrigenPiso = origenPiso;
        envio.OrigenDepartamento = origenDepartamento;
        envio.OrigenLocalidad = origenLocalidad;
        envio.OrigenProvincia = origenProvincia;
        envio.OrigenLat = origenLat;
        envio.OrigenLng = origenLng;
        envio.DestinoCalle = destinoCalle;
        envio.DestinoAltura = destinoAltura;
        envio.DestinoPiso = destinoPiso;
        envio.DestinoDepartamento = destinoDepartamento;
        envio.DestinoLocalidad = destinoLocalidad;
        envio.DestinoProvincia = destinoProvincia;
        envio.DestinoLat = destinoLat;
        envio.DestinoLng = destinoLng;
        envio.FechaLimite = fechaLimite;
        envio.PesoKg = pesoKg;
        envio.Tamanio = tamanio;
        envio.Descripcion = descripcion.Trim();
        envio.Monto = monto;

        await _context.SaveChangesAsync();
        return envio;
    }

    public async Task<bool> Borrar(int id, int usuarioId)
    {
        var envio = await _context.Envios.FirstOrDefaultAsync(e =>
            e.Id == id && e.UsuarioId == usuarioId
        );
        if (envio is null)
        {
            return false;
        }

        _context.Envios.Remove(envio);
        await _context.SaveChangesAsync();
        return true;
    }
}

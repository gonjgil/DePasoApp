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
        string origenDir,
        decimal origenLat,
        decimal origenLng,
        string destinoDir,
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
            OrigenDir = origenDir.Trim(),
            OrigenLat = origenLat,
            OrigenLng = origenLng,
            DestinoDir = destinoDir.Trim(),
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

    public async Task<Envios?> ObtenerPorId(int id, int usuarioId)
    {
        return await _context.Envios.FirstOrDefaultAsync(e =>
            e.Id == id && e.UsuarioId == usuarioId
        );
    }

    public async Task<Envios?> Editar(
        int id,
        int usuarioId,
        string origenDir,
        decimal origenLat,
        decimal origenLng,
        string destinoDir,
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

        envio.OrigenDir = origenDir.Trim();
        envio.OrigenLat = origenLat;
        envio.OrigenLng = origenLng;
        envio.DestinoDir = destinoDir.Trim();
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

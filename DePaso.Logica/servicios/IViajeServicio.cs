using DePaso.Logica.dtos;

namespace DePaso.Logica.servicios;

public interface IViajeServicio
{
    Task<IReadOnlyList<ViajeResumenDto>> ListarAsync(int usuarioId, FiltroViajes filtro, CancellationToken ct = default);

    Task<Resultado<ViajeDetalleDto>> ObtenerAsync(int id, int usuarioId, CancellationToken ct = default);

    Task<Resultado<ViajeDetalleDto>> CrearAsync(ViajeDatos datos, CancellationToken ct = default);

    Task<Resultado<ViajeDetalleDto>> ActualizarAsync(int id, ViajeDatos datos, CancellationToken ct = default);

    Task<Resultado<ViajeDetalleDto>> DesactivarAsync(int id, int usuarioId, CancellationToken ct = default);

    Task<Resultado<ViajeDetalleDto>> ActivarAsync(int id, int usuarioId, CancellationToken ct = default);

    Task<Resultado<RutaDto>> CalcularRutaAsync(
        DireccionDatos origen,
        DireccionDatos destino,
        IReadOnlyList<ParadaDatos> paradas,
        CancellationToken ct = default);

    Task<Resultado<IReadOnlyList<ParadaDto>>> ListarParadasAsync(int id, int usuarioId, CancellationToken ct = default);

    Task<Resultado<ViajeDetalleDto>> AgregarParadaAsync(int id, int usuarioId, ParadaDatos parada, CancellationToken ct = default);

    Task<Resultado<ViajeDetalleDto>> ModificarParadaAsync(
        int id,
        int paradaId,
        int usuarioId,
        DireccionDatos direccion,
        CancellationToken ct = default);

    Task<Resultado<ViajeDetalleDto>> EliminarParadaAsync(int id, int paradaId, int usuarioId, CancellationToken ct = default);

    Task<Resultado<ViajeDetalleDto>> ReordenarParadasAsync(
        int id,
        int usuarioId,
        IReadOnlyList<int> paradaIds,
        CancellationToken ct = default);

    Task<Resultado<IReadOnlyList<PaqueteAsignadoDto>>> ListarPaquetesAsync(int id, int usuarioId, CancellationToken ct = default);
}
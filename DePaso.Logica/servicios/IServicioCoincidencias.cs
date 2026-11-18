using DePaso.Logica.dtos;

namespace DePaso.Logica.servicios;

public interface IServicioCoincidencias
{
    Task<Resultado<ListaCoincidenciasDto>> ListarAsync(int viajeId, int usuarioId, DateOnly fecha, CancellationToken ct = default);

    Task<Resultado<DetalleCoincidenciaDto>> DetalleAsync(int viajeId, int envioId, int usuarioId, DateOnly fecha, CancellationToken ct = default);

    Task<Resultado<OptimizacionCoincidenciasDto>> OptimizarAsync(int viajeId, int usuarioId, DateOnly fecha, CancellationToken ct = default);

    Task<Resultado<ResultadoAceptacionDto>> AceptarAsync(int viajeId, int usuarioId, DateOnly fecha, IReadOnlyList<int> envioIds, CancellationToken ct = default);
}

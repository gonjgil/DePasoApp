using DePaso.Logica.dtos;

namespace DePaso.Logica.servicios;

public interface ITransportesServicio
{
    Task<IReadOnlyList<TransporteDto>> ListarAsync(int usuarioId, CancellationToken ct = default);

    Task<Resultado<TransporteDto>> AvanzarAsync(int asignacionId, int usuarioId, byte estadoEsperado, CancellationToken ct = default);

    /// <summary>Cancela la asignación y devuelve el envío a la lista de oportunidades.</summary>
    Task<Resultado<bool>> CancelarAsync(int asignacionId, int usuarioId, byte estadoEsperado, CancellationToken ct = default);
}

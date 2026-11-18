using DePaso.Logica.dtos;

namespace DePaso.Logica.servicios;

public interface IOportunidadesServicio
{
    Task<IReadOnlyList<OportunidadSalidaDto>> ListarSalidasAsync(int usuarioId, CancellationToken ct = default);
}

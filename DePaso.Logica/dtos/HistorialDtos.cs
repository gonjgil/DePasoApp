namespace DePaso.Logica.Dtos;

public sealed record OperacionHistorialDto(
    string Origen,
    string Destino,
    string Detalle,
    decimal Ganancia,
    DateOnly Fecha
);

public sealed record HistorialDto(
    int ViajesRealizados,
    int EnviosTransportados,
    decimal GananciaEfectiva,
    IReadOnlyList<OperacionHistorialDto> Operaciones
);

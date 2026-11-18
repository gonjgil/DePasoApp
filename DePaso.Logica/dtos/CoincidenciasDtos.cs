using DePaso.Logica.enums;

namespace DePaso.Logica.dtos;

public sealed record CapacidadDto(int Paquetes, decimal PesoKg, decimal DesvioKm);

public sealed record RutaResumenDto(decimal Km, int Minutos);

/// <summary>Envío que coincide con una salida. Pago = Monto del envío.</summary>
public sealed record CoincidenciaDto(
    int EnvioId,
    string Origen,
    string Destino,
    decimal KilosKg,
    decimal DesvioKm,
    decimal Pago,
    NivelCoincidencia Nivel,
    string NivelTexto
);

public sealed record ListaCoincidenciasDto(
    int ViajeId,
    string Viaje,
    DateOnly Fecha,
    CapacidadDto Capacidad,
    IReadOnlyList<CoincidenciaDto> Coincidencias
);

public sealed record DetalleCoincidenciaDto(
    int EnvioId,
    string Origen,
    string Destino,
    RutaResumenDto RutaHabitual,
    RutaResumenDto RutaConEnvio,
    decimal DesvioKm,
    decimal KilosKg,
    CapacidadDto CapacidadQueda,
    decimal Pago,
    bool PuedeAceptarse,
    IReadOnlyList<string> Motivos
);

public sealed record CombinacionDto(
    IReadOnlyList<int> EnvioIds,
    int Paquetes,
    decimal KilosKg,
    decimal DesvioKm,
    decimal Pago
);

/// <summary>Clasificación de un envío evaluado para la combinación. Motivos explica por qué no entra.</summary>
public sealed record OpcionCoincidenciaDto(
    int EnvioId,
    string Origen,
    string Destino,
    decimal KilosKg,
    decimal DesvioKm,
    decimal Pago,
    EstadoOpcionCoincidencia Estado,
    string EstadoTexto,
    IReadOnlyList<string> Motivos
);

/// <summary>Envío que no se pudo reservar y el motivo.</summary>
public sealed record FallaAceptacionDto(int EnvioId, string Motivo);

/// <summary>
/// Resultado de aceptar uno o varios envíos. Los libres se reservan; los tomados por otro conductor
/// y los que ya no entran se devuelven con su motivo.
/// </summary>
public sealed record ResultadoAceptacionDto(
    int ViajeId,
    DateOnly Fecha,
    IReadOnlyList<int> Aceptados,
    IReadOnlyList<FallaAceptacionDto> Tomados,
    IReadOnlyList<FallaAceptacionDto> NoEntran
);

public sealed record OportunidadSalidaDto(
    int ViajeId,
    string Origen,
    string Destino,
    DateOnly Fecha,
    TimeOnly HoraSalida,
    int Coincidencias
);

public sealed record TransporteDto(
    int AsignacionId,
    int EnvioId,
    string Origen,
    string Destino,
    decimal KilosKg,
    decimal Pago,
    DateOnly FechaViaje,
    TimeOnly HoraSalida,
    byte Estado,
    string EstadoTexto,
    byte? Siguiente,
    string? SiguienteTexto
);

public sealed record OptimizacionCoincidenciasDto(
    int ViajeId,
    DateOnly Fecha,
    CombinacionDto Recomendada,
    IReadOnlyList<OpcionCoincidenciaDto> Opciones,
    bool TopeAlcanzado,
    int TopeCandidatos
);

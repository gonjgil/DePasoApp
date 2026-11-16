using DePaso.Logica.enums;

namespace DePaso.Logica.dtos;

public sealed record DireccionDatos(
       string Calle,
       int Altura,
       string? Piso,
       string? Departamento,
       string Localidad,
       string Provincia,
       decimal? Lat = null,
       decimal? Lng = null);

public sealed record ParadaDatos(DireccionDatos Direccion, int? Orden = null);

public sealed record ViajeDatos(
    int UsuarioId,
    DireccionDatos Origen,
    DireccionDatos Destino,
    IReadOnlyList<byte> Dias,
    TimeOnly HoraSalida,
    TimeOnly HoraLlegada,
    int MaxPaquetes,
    decimal PesoMaxKg,
    decimal DesvioMaxKm,
    IReadOnlyList<ParadaDatos> Paradas);

public enum FiltroViajes
{
    Todos = 0,
    Activos = 1,
    Inactivos = 2,
}

public sealed record DireccionDto(
    string Calle,
    int Altura,
    string? Piso,
    string? Departamento,
    string Localidad,
    string Provincia,
    decimal Lat,
    decimal Lng,
    string Texto);

public sealed record ParadaDto(int Id, int Orden, DireccionDto Direccion);

public sealed record PaqueteAsignadoDto(
    int AsignacionId,
    int EnvioId,
    string Descripcion,
    string Origen,
    string Destino,
    DateOnly FechaViaje,
    decimal PesoKg,
    decimal Ganancia,
    decimal DesvioKm,
    EstadoEnvio Estado,
    string EstadoTexto);

public sealed record ViajeResumenDto(
    int Id,
    string Origen,
    string Destino,
    List<byte> Dias,
    string DiasTexto,
    TimeOnly HoraSalida,
    TimeOnly HoraLlegada,
    int MaxPaquetes,
    decimal PesoMaxKg,
    decimal DesvioMaxKm,
    decimal DistanciaKm,
    int DuracionMin,
    bool Activo,
    int CantidadParadas,
    int PaquetesEnCurso);

public sealed record ViajeDetalleDto(
    int Id,
    int UsuarioId,
    DireccionDto Origen,
    DireccionDto Destino,
    List<byte> Dias,
    string DiasTexto,
    TimeOnly HoraSalida,
    TimeOnly HoraLlegada,
    int MaxPaquetes,
    decimal PesoMaxKg,
    decimal DesvioMaxKm,
    decimal DistanciaKm,
    int DuracionMin,
    bool Activo,
    List<ParadaDto> Paradas,
    List<PaqueteAsignadoDto> PaquetesAsignados);

public sealed record PuntoRutaDto(string Tipo, int Orden, string Texto, decimal Lat, decimal Lng);

public sealed record RutaDto(
    decimal DistanciaKm,
    int DuracionMin,
    bool Estimada,
    List<PuntoRutaDto> Puntos,
    string? Advertencia);
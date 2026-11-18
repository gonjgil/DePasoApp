using System;
using System.Collections.Generic;
using System.Text;

using DePaso.Logica.enums;

namespace DePaso.Logica.Dtos
{
    public sealed record ViajeProximoDto(
    int Id,
    string Origen,
    string Destino,
    string DiasTexto,
    TimeOnly HoraSalida,
    TimeOnly HoraLlegada,
    DateOnly ProximaFecha);

    public sealed record EnvioPendienteDto(
        int Id,
        string Origen,
        string Destino,
        string Descripcion,
        decimal Monto,
        EstadoEnvio Estado,
        string EstadoTexto,
        DateOnly FechaLimite);

    public sealed record DashboardResumenDto(
        string NombreUsuario,
        int ProximosViajes,
        int EnviosPendientes,
        int Oportunidades,
        decimal PotencialMes,
        IReadOnlyList<ViajeProximoDto> ListaProximosViajes,
        IReadOnlyList<EnvioPendienteDto> ListaEnviosPendientes);

    public sealed record PotencialSemanaDto(
    DateOnly Desde,
    DateOnly Hasta,
    decimal Ganancia,
    decimal DesvioKm,
    int CantidadEnvios);

    public sealed record PotencialRecorridoDto(
        int ViajeId,
        string Origen,
        string Destino,
        decimal Ganancia);

    public sealed record PotencialMesDto(
        string Criterio,
        string Mes,
        decimal GananciaTotal,
        decimal DesvioTotalKm,
        int CantidadEnvios,
        int ViajesAnalizados,
        decimal PromedioPorViaje,
        IReadOnlyList<PotencialSemanaDto> Semanas,
        PotencialRecorridoDto? MejorRecorrido);
}
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
}
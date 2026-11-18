namespace DePaso.Api.Models;

/// <summary>Envíos que el conductor quiere reservar para una salida. Uno solo para "Aceptar" individual.</summary>
public sealed record AceptarCoincidenciasModelo(int UsuarioId, DateOnly Fecha, IReadOnlyList<int> EnvioIds);

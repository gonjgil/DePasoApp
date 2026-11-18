namespace DePaso.Logica.enums;

public enum NivelCoincidencia : byte
{
    MuyCompatible = 0,
    Compatible = 1,
    MuchoDesvio = 2,
}

public enum EstadoOpcionCoincidencia : byte
{
    Recomendado = 0,
    Alternativo = 1,
    NoEntra = 2,
}

public static class NivelesCoincidencia
{
    public static string Texto(NivelCoincidencia nivel) => nivel switch
    {
        NivelCoincidencia.MuyCompatible => "Muy compatible",
        NivelCoincidencia.Compatible => "Compatible",
        NivelCoincidencia.MuchoDesvio => "Mucho desvío",
        _ => "Desconocido",
    };

    public static string Texto(EstadoOpcionCoincidencia estado) => estado switch
    {
        EstadoOpcionCoincidencia.Recomendado => "Recomendado",
        EstadoOpcionCoincidencia.Alternativo => "Alternativo",
        EstadoOpcionCoincidencia.NoEntra => "No entra",
        _ => "Desconocido",
    };
}

namespace DePaso.Logica.enums
{
    public enum EstadoEnvio : byte
    {
        BuscandoConductor = 0,
        ConductorAsignado = 1,
        Retirado = 2,
        EnCamino = 3,
        Entregado = 4,
        Cancelado = 5
    }

    public static class EstadosEnvio
    {
        public static string Texto(byte estado) => (EstadoEnvio)estado switch
        {
            EstadoEnvio.BuscandoConductor => "Buscando conductor",
            EstadoEnvio.ConductorAsignado => "Conductor asignado",
            EstadoEnvio.Retirado => "Retirado",
            EstadoEnvio.EnCamino => "En camino",
            EstadoEnvio.Entregado => "Entregado",
            EstadoEnvio.Cancelado => "Cancelado",
            _ => "Desconocido"
        };
    }
}

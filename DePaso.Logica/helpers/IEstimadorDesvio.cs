namespace DePaso.Logica.helpers
{
    public readonly record struct Coordenada(double Lat, double Lng);

    public interface IEstimadorDesvio
    {
        double DistanciaKm(Coordenada a, Coordenada b);
        double CalcularDesvioKm(IReadOnlyList<Coordenada> rutaBase, Coordenada origenEnvio, Coordenada destinoEnvio);
    }
}
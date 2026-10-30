namespace DePaso.Logica.helpers
{
    public sealed class EstimadorDesvioHaversine : IEstimadorDesvio
    {
        private const double RadioTierraKm = 6371.0088;

        public double DistanciaKm(Coordenada a, Coordenada b)
        {
            var dLat = Radianes(b.Lat - a.Lat);
            var dLng = Radianes(b.Lng - a.Lng);
            var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(Radianes(a.Lat)) * Math.Cos(Radianes(b.Lat)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return 2 * RadioTierraKm * Math.Asin(Math.Min(1, Math.Sqrt(h)));
        }

        public double CalcularDesvioKm(IReadOnlyList<Coordenada> rutaBase, Coordenada origenEnvio, Coordenada destinoEnvio)
        {
            if (rutaBase.Count < 2)
            {
                throw new ArgumentException("La ruta base necesita al menos origen y destino.", nameof(rutaBase));
            }

            var segmentos = rutaBase.Count - 1;
            var mejor = double.MaxValue;

            for (var i = 0; i < segmentos; i++)
            {
                var extraRetiro = Extra(rutaBase[i], rutaBase[i + 1], origenEnvio);

                var mismoSegmento = DistanciaKm(rutaBase[i], origenEnvio)
                                    + DistanciaKm(origenEnvio, destinoEnvio)
                                    + DistanciaKm(destinoEnvio, rutaBase[i + 1])
                                    - DistanciaKm(rutaBase[i], rutaBase[i + 1]);
                mejor = Math.Min(mejor, mismoSegmento);

                for (var j = i + 1; j < segmentos; j++)
                {
                    mejor = Math.Min(mejor, extraRetiro + Extra(rutaBase[j], rutaBase[j + 1], destinoEnvio));
                }
            }

            return Math.Max(0, mejor);
        }

        private double Extra(Coordenada a, Coordenada b, Coordenada p) =>
            DistanciaKm(a, p) + DistanciaKm(p, b) - DistanciaKm(a, b);

        private static double Radianes(double grados) => grados * Math.PI / 180.0;
    }
}

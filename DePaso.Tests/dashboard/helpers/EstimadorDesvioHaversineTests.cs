using DePaso.Logica.helpers;

namespace DePaso.Tests.dashboard.helpers
{
    public class EstimadorDesvioHaversineTests
    {
        private readonly EstimadorDesvioHaversine _estimador = new();
        private static Coordenada P(double lat, double lng) => new(lat, lng);

        [Fact]
        public void Distancia_MismoPunto_EsCero()
        {
            Assert.Equal(0, _estimador.DistanciaKm(P(-34.6, -58.4), P(-34.6, -58.4)), 6);
        }

        [Fact]
        public void Distancia_UnGradoDeLatitud_SonUnos111Km()
        {
            var km = _estimador.DistanciaKm(P(-34.0, -58.4), P(-35.0, -58.4));
            Assert.InRange(km, 111.0, 111.4);
        }

        [Fact]
        public void Distancia_EsSimetrica()
        {
            var ida = _estimador.DistanciaKm(P(-34.5, -58.4), P(-34.7, -58.1));
            var vuelta = _estimador.DistanciaKm(P(-34.7, -58.1), P(-34.5, -58.4));
            Assert.Equal(ida, vuelta, 9);
        }

        [Fact]
        public void Desvio_PaqueteSobreLaRuta_EsCasiCero()
        {
            var ruta = new[] { P(-34.5, -58.4), P(-34.7, -58.4) };
            var km = _estimador.CalcularDesvioKm(ruta, P(-34.55, -58.4), P(-34.65, -58.4));
            Assert.InRange(km, 0, 0.01);
        }

        [Fact]
        public void Desvio_PaqueteAlcostadoDeLaRuta_EsMayorQueCero()
        {
            var ruta = new[] { P(-34.5, -58.4), P(-34.7, -58.4) };
            var km = _estimador.CalcularDesvioKm(ruta, P(-34.55, -58.3), P(-34.65, -58.3));
            Assert.InRange(km, 9.0, 11.5);
        }

        [Fact]
        public void Desvio_ElRetiroDebeSerAntesQueLaEntrega()
        {
            var ruta = new[] { P(-34.5, -58.4), P(-34.7, -58.4) };
            var km = _estimador.CalcularDesvioKm(ruta, P(-34.65, -58.4), P(-34.55, -58.4));
            Assert.True(km > 20, $"Se esperaba un desvío grande y fue {km} km");
        }

        [Fact]
        public void Desvio_ConParada_RetiroYEntregaEnElMismoTramo()
        {
            var ruta = new[] { P(-34.5, -58.4), P(-34.6, -58.4), P(-34.7, -58.4) };
            var km = _estimador.CalcularDesvioKm(ruta, P(-34.62, -58.4), P(-34.68, -58.4));
            Assert.InRange(km, 0, 0.01);
        }

        [Fact]
        public void Desvio_ConParada_RetiroYEntregaEnTramosDistintos()
        {
            var ruta = new[] { P(-34.5, -58.4), P(-34.6, -58.4), P(-34.7, -58.4) };
            var km = _estimador.CalcularDesvioKm(ruta, P(-34.55, -58.4), P(-34.65, -58.4));
            Assert.InRange(km, 0, 0.01);
        }

        [Fact]
        public void Desvio_NuncaEsNegativo()
        {
            var ruta = new[] { P(-34.5, -58.4), P(-34.7, -58.4) };
            var km = _estimador.CalcularDesvioKm(ruta, P(-34.5, -58.4), P(-34.7, -58.4));
            Assert.True(km >= 0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Desvio_ConRutaIncompleta_Lanza(int puntos)
        {
            var ruta = Enumerable.Range(0, puntos).Select(_ => P(-34.5, -58.4)).ToList();
            Assert.Throws<ArgumentException>(() => _estimador.CalcularDesvioKm(ruta, P(-34.5, -58.4), P(-34.6, -58.4)));
        }
    }
}
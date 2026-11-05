using DePaso.Logica.helpers;

namespace DePaso.Tests.dashboard.helpers
{
    public class OptimizadorCombinacionesTests
    {
        private readonly OptimizadorCombinaciones _optimizador = new();

        private static CandidatoEnvio C(int id, decimal ganancia, decimal peso, decimal desvio) => new(id, ganancia, peso, desvio);

        private static List<CandidatoEnvio> Candidatos() => new()
    {
        C(1, 4000, 2, 1m),  
        C(2, 5500, 6, 2m), 
        C(3, 3500, 4, 1m),
        C(4, 7000, 8, 3m), 
        C(5, 2500, 1, 0.5m)  
    };

        private static List<int> Ids(CombinacionOptima c) => c.Envios.Select(e => e.EnvioId).OrderBy(i => i).ToList();

        [Fact]
        public void SeleccionaEnviosConMayorGananciaDentroDeLosLimites()
        {
            var r = _optimizador.Optimizar(Candidatos(), new LimitesViaje(3, 10, 5));

            Assert.Equal(new[] { 1, 2, 5 }, Ids(r));
            Assert.Equal(12000m, r.Ganancia);
            Assert.Equal(9m, r.PesoKg);
            Assert.Equal(3.5m, r.DesvioKm);
            Assert.Equal(3, r.Cantidad);
        }

        [Fact]
        public void RespetaCantidadMaximaDePaquetes()
        {
            var r = _optimizador.Optimizar(Candidatos(), new LimitesViaje(2, 100, 100));

            Assert.Equal(new[] { 2, 4 }, Ids(r));
            Assert.Equal(12500m, r.Ganancia);
        }

        [Fact]
        public void RespetaPesoMaximo()
        {
            var r = _optimizador.Optimizar(Candidatos(), new LimitesViaje(5, 3, 100));
            Assert.Equal(new[] { 1, 5 }, Ids(r));
            Assert.True(r.PesoKg <= 3);
        }

        [Fact]
        public void RespetaDesvioMaximoAcumulado()
        {
            var r = _optimizador.Optimizar(Candidatos(), new LimitesViaje(5, 100, 1));
            Assert.Equal(new[] { 1 }, Ids(r));
            Assert.True(r.DesvioKm <= 1);
        }

        [Fact]
        public void EnEmpateDeGananciaGanaMenorDesvio()
        {
            var candidatos = new List<CandidatoEnvio> { C(10, 5000, 1, 3m), C(11, 5000, 1, 1m) };
            var r = _optimizador.Optimizar(candidatos, new LimitesViaje(1, 10, 10));
            Assert.Equal(new[] { 11 }, Ids(r));
        }

        [Fact]
        public void EnEmpateDeGananciaPrefiereCombinacionDeMenorDesvio()
        {
            var candidatos = new List<CandidatoEnvio>
        {
            C(20, 5000, 10, 1m),  
            C(21, 3000, 5, 1m),
            C(22, 2000, 5, 1m)   
        };

            var r = _optimizador.Optimizar(candidatos, new LimitesViaje(2, 10, 10));
            Assert.Equal(new[] { 20 }, Ids(r));
        }

        [Fact]
        public void SinCandidatos_DevuelveCombinacionVacia()
        {
            var r = _optimizador.Optimizar(new List<CandidatoEnvio>(), new LimitesViaje(3, 10, 5));
            Assert.Empty(r.Envios);
            Assert.Equal(0m, r.Ganancia);
        }

        [Theory]
        [InlineData(0, 10, 5)]
        [InlineData(-1, 10, 5)]
        [InlineData(3, -1, 5)]
        [InlineData(3, 10, -1)]
        public void ConLimitesAgotados_DevuelveCombinacionVacia(int paquetes, int peso, int desvio)
        {
            var r = _optimizador.Optimizar(Candidatos(), new LimitesViaje(paquetes, peso, desvio));
            Assert.Empty(r.Envios);
        }

        [Fact]
        public void IgnoraCandidatosQueNoEntranIndividualmenteOSinGanancia()
        {
            var candidatos = new List<CandidatoEnvio>
        {
            C(1, 9000, 11, 1m), 
            C(2, 9000, 1, 6m),  
            C(3, 0, 1, 0m),   
            C(4, 100, 1, 1m)
        };

            var r = _optimizador.Optimizar(candidatos, new LimitesViaje(3, 10, 5));
            Assert.Equal(new[] { 4 }, Ids(r));
        }

        [Fact]
        public void ConMasCandidatosQueElTope_UsaLosDeMayorGanancia()
        {
            var candidatos = Enumerable.Range(1, 40).Select(i => C(i, 100 + i, 1, 0.1m)).ToList();
            var r = _optimizador.Optimizar(candidatos, new LimitesViaje(3, 100, 100));
            Assert.Equal(new[] { 38, 39, 40 }, Ids(r));
        }

        [Fact]
        public void CoincideConFuerzaBrutaEnCasosAleatorios()
        {
            var azar = new Random(42);
            for (var caso = 0; caso < 60; caso++)
            {
                var n = azar.Next(1, 13);
                var candidatos = Enumerable.Range(1, n)
                    .Select(i => C(i, azar.Next(1, 10) * 1000m, azar.Next(1, 7), azar.Next(1, 7) / 2m))
                    .ToList();
                var limites = new LimitesViaje(azar.Next(1, 5), azar.Next(3, 15), azar.Next(1, 8));

                var esperado = FuerzaBruta(candidatos, limites);
                var real = _optimizador.Optimizar(candidatos, limites);

                Assert.Equal(esperado.Ganancia, real.Ganancia);
                Assert.Equal(esperado.Desvio, real.DesvioKm);
                Assert.True(real.Cantidad <= limites.MaxPaquetes);
                Assert.True(real.PesoKg <= limites.PesoMaxKg);
                Assert.True(real.DesvioKm <= limites.DesvioMaxKm);
            }
        }

        private static (decimal Ganancia, decimal Desvio) FuerzaBruta(List<CandidatoEnvio> c, LimitesViaje l)
        {
            decimal mejorG = 0, mejorD = 0;
            for (var mascara = 0; mascara < (1 << c.Count); mascara++)
            {
                decimal g = 0, p = 0, d = 0;
                var cantidad = 0;
                for (var i = 0; i < c.Count; i++)
                {
                    if ((mascara & (1 << i)) == 0) continue;
                    cantidad++;
                    g += c[i].Ganancia;
                    p += c[i].PesoKg;
                    d += c[i].DesvioKm;
                }

                if (cantidad > l.MaxPaquetes || p > l.PesoMaxKg || d > l.DesvioMaxKm) continue;
                if (g > mejorG || (g == mejorG && d < mejorD))
                {
                    mejorG = g;
                    mejorD = d;
                }
            }

            return (mejorG, mejorD);
        }
    }

}

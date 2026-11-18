using DePaso.Logica.helpers;

namespace DePaso.Tests.dashboard.helpers
{
    public class OptimizadorPotencialTests
    {
        private readonly OptimizadorCombinaciones _optimizador = new();

        private static CandidatoEnvio C(int id, decimal ganancia, decimal peso, decimal desvio) =>
            new(id, ganancia, peso, desvio);

        private static List<CandidatoEnvio> DelEnunciado() => new()
        {
            C(1, 4000, 2, 1m),
            C(2, 5500, 6, 2m),
            C(3, 3500, 4, 1m),
            C(4, 7000, 8, 3m),
            C(5, 2500, 1, 0.5m),
        };

        private static List<int> Ids(CombinacionOptima c) =>
            c.Envios.Select(e => e.EnvioId).OrderBy(i => i).ToList();

        [Fact]
        public void Ganancia_DelegaEnElOptimizadorYEligeLaMismaCombinacion()
        {
            var limites = new LimitesViaje(3, 10, 5);
            var directo = _optimizador.Optimizar(DelEnunciado(), limites);
            var porCriterio = OptimizadorPotencial.Optimizar(DelEnunciado(), limites, "ganancia", _optimizador);

            Assert.Equal(Ids(directo), Ids(porCriterio));
            Assert.Equal(12000m, porCriterio.Ganancia);
            Assert.Equal(new[] { 1, 2, 5 }, Ids(porCriterio));
        }

        [Fact]
        public void Desvio_EligeLaCombinacionDeMenorDesvio()
        {
            var r = OptimizadorPotencial.Optimizar(DelEnunciado(), new LimitesViaje(3, 10, 5), "desvio", _optimizador);

            Assert.Equal(new[] { 5 }, Ids(r));
            Assert.Equal(0.5m, r.DesvioKm);
        }

        [Fact]
        public void Desvio_EnEmpateSeQuedaConLaMayorGananciaDentroDelTope()
        {
            var candidatos = new List<CandidatoEnvio>
            {
                C(1, 100, 1, 0m),
                C(2, 300, 1, 0m),
                C(3, 200, 1, 0m),
            };

            var r = OptimizadorPotencial.Optimizar(candidatos, new LimitesViaje(2, 10, 10), "desvio", _optimizador);

            Assert.Equal(new[] { 2, 3 }, Ids(r));
            Assert.Equal(0m, r.DesvioKm);
            Assert.Equal(500m, r.Ganancia);
        }

        [Fact]
        public void Equilibrio_PrefiereMasGananciaPorCadaKmDeDesvio()
        {
            var r = OptimizadorPotencial.Optimizar(DelEnunciado(), new LimitesViaje(3, 10, 5), "equilibrio", _optimizador);

            Assert.Equal(new[] { 1, 3, 5 }, Ids(r));
            Assert.Equal(10000m, r.Ganancia);
            Assert.Equal(2.5m, r.DesvioKm);
        }

        [Fact]
        public void CriterioDesconocido_SeTrataComoGanancia()
        {
            var limites = new LimitesViaje(3, 10, 5);
            var r = OptimizadorPotencial.Optimizar(DelEnunciado(), limites, "otro", _optimizador);

            Assert.Equal(new[] { 1, 2, 5 }, Ids(r));
        }
    }
}

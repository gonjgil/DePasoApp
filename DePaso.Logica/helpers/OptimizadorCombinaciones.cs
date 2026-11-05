namespace DePaso.Logica.helpers
{
    public sealed class OptimizadorCombinaciones : IOptimizadorCombinaciones
    {
        public const int MAX_CANDIDATOS = 25;

        public CombinacionOptima Optimizar(IReadOnlyList<CandidatoEnvio> candidatos, LimitesViaje limites)
        {
            if (limites.MaxPaquetes <= 0)
            {
                return CombinacionOptima.Vacia;
            }

            var factibles = candidatos
                .Where(c => c.Ganancia > 0 && c.PesoKg <= limites.PesoMaxKg && c.DesvioKm <= limites.DesvioMaxKm)
                .OrderByDescending(c => c.Ganancia)
                .ThenBy(c => c.DesvioKm)
                .ThenBy(c => c.EnvioId)
                .Take(MAX_CANDIDATOS)
                .ToList();

            if (factibles.Count == 0)
            {
                return CombinacionOptima.Vacia;
            }

            var mejor = new Mejor();
            Explorar(factibles, 0, new List<CandidatoEnvio>(), 0m, 0m, 0m, limites, mejor);
            return new CombinacionOptima(mejor.Envios);
        }

        private static void Explorar(
            List<CandidatoEnvio> candidatos, int desde, List<CandidatoEnvio> actual,
            decimal ganancia, decimal peso, decimal desvio, LimitesViaje limites, Mejor mejor)
        {
            if (ganancia > mejor.Ganancia || (ganancia == mejor.Ganancia && desvio < mejor.Desvio))
            {
                mejor.Envios = actual.ToList();
                mejor.Ganancia = ganancia;
                mejor.Desvio = desvio;
            }

            var restantes = limites.MaxPaquetes - actual.Count;
            if (restantes <= 0)
            {
                return;
            }

            var cota = ganancia;
            for (var k = desde; k < candidatos.Count && k < desde + restantes; k++)
            {
                cota += candidatos[k].Ganancia;
            }

            if (cota < mejor.Ganancia)
            {
                return;
            }

            for (var i = desde; i < candidatos.Count; i++)
            {
                var c = candidatos[i];
                if (peso + c.PesoKg > limites.PesoMaxKg || desvio + c.DesvioKm > limites.DesvioMaxKm)
                {
                    continue;
                }

                actual.Add(c);
                Explorar(candidatos, i + 1, actual, ganancia + c.Ganancia, peso + c.PesoKg, desvio + c.DesvioKm, limites, mejor);
                actual.RemoveAt(actual.Count - 1);
            }
        }

        private sealed class Mejor
        {
            public List<CandidatoEnvio> Envios { get; set; } = new();
            public decimal Ganancia { get; set; }
            public decimal Desvio { get; set; }
        }
    }
}

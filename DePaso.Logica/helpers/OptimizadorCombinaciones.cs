namespace DePaso.Logica.helpers
{
    public sealed class OptimizadorCombinaciones : IOptimizadorCombinaciones
    {
        public const int MAX_CANDIDATOS = 25;

        public CombinacionOptima Optimizar(IReadOnlyList<CandidatoEnvio> candidatos, LimitesViaje limites)
        {
            return Optimizar(candidatos, limites, "ganancia");
        }

        public CombinacionOptima Optimizar(IReadOnlyList<CandidatoEnvio> candidatos, LimitesViaje limites, string criterio)
        {
            if (limites.MaxPaquetes <= 0)
            {
                return CombinacionOptima.Vacia;
            }

            criterio = criterio == "desvio" || criterio == "equilibrio" ? criterio : "ganancia";

            var factibles = candidatos.Where(c =>
                c.Ganancia > 0 && c.PesoKg <= limites.PesoMaxKg && c.DesvioKm <= limites.DesvioMaxKm);

            var ordenados = criterio switch
            {
                "desvio" => factibles
                    .OrderBy(c => c.DesvioKm)
                    .ThenByDescending(c => c.Ganancia)
                    .ThenBy(c => c.EnvioId),
                "equilibrio" => factibles
                    .OrderByDescending(c => c.Ganancia / (1m + c.DesvioKm))
                    .ThenBy(c => c.DesvioKm)
                    .ThenBy(c => c.EnvioId),
                _ => factibles
                    .OrderByDescending(c => c.Ganancia)
                    .ThenBy(c => c.DesvioKm)
                    .ThenBy(c => c.EnvioId),
            };

            var elegidos = ordenados.Take(MAX_CANDIDATOS).ToList();
            if (elegidos.Count == 0)
            {
                return CombinacionOptima.Vacia;
            }

            var mejor = new Mejor();
            Explorar(elegidos, 0, new List<CandidatoEnvio>(), 0m, 0m, 0m, limites, mejor, criterio);
            return new CombinacionOptima(mejor.Envios);
        }

        private static void Explorar(
            List<CandidatoEnvio> candidatos, int desde, List<CandidatoEnvio> actual,
            decimal ganancia, decimal peso, decimal desvio, LimitesViaje limites, Mejor mejor, string criterio)
        {
            var cantidad = actual.Count;
            if (EsMejor(criterio, ganancia, desvio, cantidad, mejor))
            {
                mejor.Envios = actual.ToList();
                mejor.Ganancia = ganancia;
                mejor.Desvio = desvio;
                mejor.Cantidad = cantidad;
            }

            var restantes = limites.MaxPaquetes - cantidad;
            if (restantes <= 0)
            {
                return;
            }

            if (criterio == "desvio" && mejor.Cantidad > 0 && desvio > mejor.Desvio)
            {
                return;
            }

            if (criterio == "ganancia")
            {
                var cota = ganancia;
                for (var k = desde; k < candidatos.Count && k < desde + restantes; k++)
                {
                    cota += candidatos[k].Ganancia;
                }

                if (cota < mejor.Ganancia)
                {
                    return;
                }
            }

            for (var i = desde; i < candidatos.Count; i++)
            {
                var c = candidatos[i];
                if (peso + c.PesoKg > limites.PesoMaxKg || desvio + c.DesvioKm > limites.DesvioMaxKm)
                {
                    continue;
                }

                actual.Add(c);
                Explorar(candidatos, i + 1, actual, ganancia + c.Ganancia, peso + c.PesoKg, desvio + c.DesvioKm, limites, mejor, criterio);
                actual.RemoveAt(actual.Count - 1);
            }
        }

        private static bool EsMejor(string criterio, decimal ganancia, decimal desvio, int cantidad, Mejor mejor)
        {
            if (criterio == "desvio")
            {
                if (cantidad == 0)
                {
                    return false;
                }

                if (mejor.Cantidad == 0 || desvio < mejor.Desvio)
                {
                    return true;
                }

                return desvio == mejor.Desvio && ganancia > mejor.Ganancia;
            }

            if (criterio == "equilibrio")
            {
                var puntaje = ganancia / (1m + desvio);
                var mejorPuntaje = mejor.Ganancia / (1m + mejor.Desvio);
                if (puntaje > mejorPuntaje)
                {
                    return true;
                }

                return puntaje == mejorPuntaje && desvio < mejor.Desvio;
            }

            return ganancia > mejor.Ganancia || (ganancia == mejor.Ganancia && desvio < mejor.Desvio);
        }

        private sealed class Mejor
        {
            public List<CandidatoEnvio> Envios { get; set; } = new();
            public decimal Ganancia { get; set; }
            public decimal Desvio { get; set; }
            public int Cantidad { get; set; }
        }
    }
}

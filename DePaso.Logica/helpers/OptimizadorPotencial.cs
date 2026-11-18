namespace DePaso.Logica.helpers
{
    public static class OptimizadorPotencial
    {
        public static CombinacionOptima Optimizar(
            IReadOnlyList<CandidatoEnvio> candidatos,
            LimitesViaje limites,
            string criterio,
            IOptimizadorCombinaciones optimizador)
        {
            if (criterio == "ganancia")
            {
                return optimizador.Optimizar(candidatos, limites);
            }

            var factibles = candidatos.Where(c =>
                c.Ganancia > 0 &&
                c.PesoKg <= limites.PesoMaxKg &&
                c.DesvioKm <= limites.DesvioMaxKm
            );

            var ordenados = criterio == "desvio"
                ? factibles
                    .OrderBy(c => c.DesvioKm)
                    .ThenByDescending(c => c.Ganancia)
                : factibles
                    .OrderByDescending(c =>
                        c.Ganancia / (1m + c.DesvioKm))
                    .ThenBy(c => c.DesvioKm);

            var seleccionados = new List<CandidatoEnvio>();

            decimal peso = 0;
            decimal desvio = 0;

            foreach (var candidato in ordenados)
            {
                if (seleccionados.Count >= limites.MaxPaquetes)
                {
                    break;
                }

                if (peso + candidato.PesoKg > limites.PesoMaxKg ||
                    desvio + candidato.DesvioKm > limites.DesvioMaxKm)
                {
                    continue;
                }

                seleccionados.Add(candidato);

                peso += candidato.PesoKg;
                desvio += candidato.DesvioKm;
            }

            return new CombinacionOptima(seleccionados);
        }
    }
}
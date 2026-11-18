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
            var normalizado = criterio == "desvio" || criterio == "equilibrio" ? criterio : "ganancia";
            return optimizador.Optimizar(candidatos, limites, normalizado);
        }
    }
}

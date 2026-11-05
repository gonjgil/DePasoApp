namespace DePaso.Logica.helpers
{
    public sealed record CandidatoEnvio(int EnvioId, decimal Ganancia, decimal PesoKg, decimal DesvioKm);
    public sealed record LimitesViaje(int MaxPaquetes, decimal PesoMaxKg, decimal DesvioMaxKm);
    public sealed record CombinacionOptima(IReadOnlyList<CandidatoEnvio> Envios)
    {
        public static CombinacionOptima Vacia { get; } = new(Array.Empty<CandidatoEnvio>());

        public decimal Ganancia => Envios.Sum(e => e.Ganancia);
        public decimal PesoKg => Envios.Sum(e => e.PesoKg);
        public decimal DesvioKm => Envios.Sum(e => e.DesvioKm);
        public int Cantidad => Envios.Count;
    }

    public interface IOptimizadorCombinaciones
    {
        CombinacionOptima Optimizar(IReadOnlyList<CandidatoEnvio> candidatos, LimitesViaje limites);
    }
}
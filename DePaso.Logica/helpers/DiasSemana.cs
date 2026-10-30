namespace DePaso.Logica.helpers
{
    public static class DiasSemana
    {
        private static readonly string[] Abreviaturas = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
        public static byte Numero(DayOfWeek dia) => dia == DayOfWeek.Sunday ? (byte)7 : (byte)dia;

        public static string Formatear(IEnumerable<byte> dias) =>
            string.Join(", ", dias
                .Where(d => d is >= 1 and <= 7)
                .Distinct()
                .OrderBy(d => d)
                .Select(d => Abreviaturas[d - 1]));
    }
}

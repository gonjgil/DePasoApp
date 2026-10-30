using DePaso.Logica.enums;
using DePaso.Logica.helpers;

namespace DePaso.Tests.dashboard.helpers
{
    public class EstadosYDiasTests
    {
        [Theory]
        [InlineData(0, "Buscando conductor")]
        [InlineData(1, "Conductor asignado")]
        [InlineData(2, "Retirado")]
        [InlineData(3, "En camino")]
        [InlineData(4, "Entregado")]
        [InlineData(5, "Cancelado")]
        [InlineData(99, "Desconocido")]
        public void TextoDeEstado(byte estado, string esperado)
        {
            Assert.Equal(esperado, EstadosEnvio.Texto(estado));
        }

        [Theory]
        [InlineData(DayOfWeek.Monday, 1)]
        [InlineData(DayOfWeek.Wednesday, 3)]
        [InlineData(DayOfWeek.Saturday, 6)]
        [InlineData(DayOfWeek.Sunday, 7)]
        public void NumeroDeDia_EsElEsperado(DayOfWeek dia, byte esperado)
        {
            Assert.Equal(esperado, DiasSemana.Numero(dia));
        }

        [Fact]
        public void Formatear_OrdenaSinRepetirEIgnoraInvalidos()
        {
            Assert.Equal("Lun, Mié, Vie", DiasSemana.Formatear(new byte[] { 5, 1, 3, 3, 9, 0 }));
        }

        [Fact]
        public void Formatear_SemanaCompleta()
        {
            Assert.Equal("Lun, Mar, Mié, Jue, Vie, Sáb, Dom", DiasSemana.Formatear(new byte[] { 1, 2, 3, 4, 5, 6, 7 }));
        }

        [Fact]
        public void Formatear_SinDias_EsVacio()
        {
            Assert.Equal("", DiasSemana.Formatear(Array.Empty<byte>()));
        }
    }

}

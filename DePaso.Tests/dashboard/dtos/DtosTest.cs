using DePaso.Logica.Dtos;
using DePaso.Logica.enums;
using DePaso.Logica.helpers;

namespace DePaso.Tests.dashboard.Dtos
{
    public class DtosTests
    {
        [Fact]
        public void LosDtosSeComparanPorValorYSePuedenCopiar()
        {
            var viaje = new ViajeProximoDto(1, "A", "B", "Lun", new TimeOnly(8, 0), new TimeOnly(8, 55), new DateOnly(2026, 10, 28));
            var envio = new EnvioPendienteDto(1, "A", "B", "Paquete", 1000m, EstadoEnvio.BuscandoConductor, "Buscando conductor", new DateOnly(2026, 11, 1));
            var resumen = new DashboardResumenDto("Juan", 1, 1, 1, 1000m, new[] { viaje }, new[] { envio });
            var candidato = new CandidatoEnvio(1, 1000m, 1m, 0.5m);
            var limites = new LimitesViaje(3, 10m, 5m);

            Assert.Equal(viaje, viaje with { });
            Assert.Equal(envio, envio with { });
            Assert.Equal(resumen, resumen with { });
            Assert.Equal(candidato, candidato with { });
            Assert.Equal(limites, limites with { });
            Assert.Equal(viaje.GetHashCode(), (viaje with { }).GetHashCode());
            Assert.Equal(envio.GetHashCode(), (envio with { }).GetHashCode());
            Assert.Equal(resumen.GetHashCode(), (resumen with { }).GetHashCode());
            Assert.Equal(candidato.GetHashCode(), (candidato with { }).GetHashCode());
            Assert.Equal(limites.GetHashCode(), (limites with { }).GetHashCode());
            Assert.False(viaje == viaje with { Id = 2 });
            Assert.False(envio == envio with { Monto = 1m });
            Assert.False(resumen == resumen with { PotencialMes = 1m });
            Assert.False(candidato == candidato with { EnvioId = 2 });
            Assert.False(limites == limites with { MaxPaquetes = 1 });
            Assert.NotEmpty(viaje.ToString());
            Assert.NotEmpty(envio.ToString());
            Assert.NotEmpty(resumen.ToString());
            Assert.NotEmpty(candidato.ToString());
            Assert.NotEmpty(limites.ToString());
            Assert.NotEmpty(CombinacionOptima.Vacia.ToString());
            Assert.True(CombinacionOptima.Vacia.Equals(CombinacionOptima.Vacia));
            Assert.True(new Coordenada(1, 2) == new Coordenada(1, 2));
            Assert.NotEmpty(new Coordenada(1, 2).ToString());
        }
    }
}

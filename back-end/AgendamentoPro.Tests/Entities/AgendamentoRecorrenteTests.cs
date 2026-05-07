using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Exceptions;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class AgendamentoRecorrenteTests
    {
        [Fact]
        public void Construtor_QuantidadeForaDoLimite_DeveLancar()
        {
            Action act1 = () => new AgendamentoRecorrente(1, 1, 1, 1,
                DayOfWeek.Monday, TimeSpan.FromHours(10),
                FrequenciaRecorrencia.Semanal, 0, DateTime.Today);
            act1.Should().Throw<DomainException>();

            Action act2 = () => new AgendamentoRecorrente(1, 1, 1, 1,
                DayOfWeek.Monday, TimeSpan.FromHours(10),
                FrequenciaRecorrencia.Semanal, 53, DateTime.Today);
            act2.Should().Throw<DomainException>();
        }

        [Fact]
        public void GerarDatas_Semanal_RetornaQuantidadeCorretaIntercaladaDe7Dias()
        {
            var inicio = new DateTime(2026, 1, 5); // 5/1/2026 = segunda
            var rec = new AgendamentoRecorrente(1, 1, 1, 1,
                DayOfWeek.Monday, TimeSpan.FromHours(10),
                FrequenciaRecorrencia.Semanal, 4, inicio);

            var datas = rec.GerarDatas().ToList();
            datas.Should().HaveCount(4);
            datas[0].Should().Be(new DateTime(2026, 1, 5));
            datas[1].Should().Be(new DateTime(2026, 1, 12));
            datas[2].Should().Be(new DateTime(2026, 1, 19));
            datas[3].Should().Be(new DateTime(2026, 1, 26));
        }

        [Fact]
        public void GerarDatas_Quinzenal_Pula14Dias()
        {
            var inicio = new DateTime(2026, 1, 5);
            var rec = new AgendamentoRecorrente(1, 1, 1, 1,
                DayOfWeek.Monday, TimeSpan.FromHours(10),
                FrequenciaRecorrencia.Quinzenal, 3, inicio);

            var datas = rec.GerarDatas().ToList();
            datas[1].Should().Be(datas[0].AddDays(14));
            datas[2].Should().Be(datas[1].AddDays(14));
        }

        [Fact]
        public void GerarDatas_Mensal_AdicionaUmMes()
        {
            var inicio = new DateTime(2026, 1, 5);
            var rec = new AgendamentoRecorrente(1, 1, 1, 1,
                DayOfWeek.Monday, TimeSpan.FromHours(10),
                FrequenciaRecorrencia.Mensal, 3, inicio);

            var datas = rec.GerarDatas().ToList();
            // Pode pular dia da semana — comportamento aceito (quando recorrência mensal,
            // exatamente um mês depois pode cair em outro DOW).
            datas[0].Should().Be(inicio);
            datas[1].Month.Should().Be(2);
            datas[2].Month.Should().Be(3);
        }

        [Fact]
        public void GerarDatas_DataInicioNaoCoincideComDOW_AjustaParaProximo()
        {
            // 5/1/2026 = segunda. Pedimos quarta-feira → ajusta pra 7/1/2026
            var inicio = new DateTime(2026, 1, 5);
            var rec = new AgendamentoRecorrente(1, 1, 1, 1,
                DayOfWeek.Wednesday, TimeSpan.FromHours(10),
                FrequenciaRecorrencia.Semanal, 2, inicio);

            var datas = rec.GerarDatas().ToList();
            datas[0].DayOfWeek.Should().Be(DayOfWeek.Wednesday);
            datas[0].Should().Be(new DateTime(2026, 1, 7));
        }

        [Fact]
        public void Cancelar_DefineRecAtivoFalse()
        {
            var rec = new AgendamentoRecorrente(1, 1, 1, 1,
                DayOfWeek.Monday, TimeSpan.FromHours(10),
                FrequenciaRecorrencia.Semanal, 4, DateTime.Today);
            rec.RecAtivo.Should().BeTrue();
            rec.Cancelar();
            rec.RecAtivo.Should().BeFalse();
        }
    }
}

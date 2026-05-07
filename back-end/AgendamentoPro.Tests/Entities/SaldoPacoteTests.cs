using AgendamentoPro.Core.Entities.Servicos;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class SaldoPacoteTests
    {
        private static (PacotePrePago pacote, SaldoPacote saldo) Novo(int qtd = 5, int validadeDias = 90)
        {
            var pacote = new PacotePrePago(rTenId: 1, rSerId: 1, "5 lavagens", qtd, 200m, validadeDias);
            // Reflexão para forçar PctId em testes (entidade não permite setter público)
            typeof(PacotePrePago).GetProperty(nameof(PacotePrePago.PctId))!
                .SetValue(pacote, 42);
            var saldo = new SaldoPacote(rTenId: 1, rCliId: 1, pacote);
            return (pacote, saldo);
        }

        [Fact]
        public void Novo_SaldoNasceComoPendente()
        {
            var (_, saldo) = Novo();
            saldo.SaldStatus.Should().Be(StatusSaldoPacote.Pendente);
            saldo.SaldQuantidadeRestante.Should().Be(5);
            saldo.SaldPagoEm.Should().BeNull();
        }

        [Fact]
        public void Pendente_NaoPodeUsarNemDebitar()
        {
            var (_, saldo) = Novo();
            saldo.PodeUsar().Should().BeFalse();
            saldo.Debitar().Should().BeFalse();
            saldo.SaldQuantidadeRestante.Should().Be(5);
        }

        [Fact]
        public void Ativar_PendenteParaAtivo_RetornaTrueEPermiteDebitar()
        {
            var (_, saldo) = Novo();
            saldo.Ativar().Should().BeTrue();
            saldo.SaldStatus.Should().Be(StatusSaldoPacote.Ativo);
            saldo.SaldPagoEm.Should().NotBeNull();
            saldo.PodeUsar().Should().BeTrue();
            saldo.Debitar().Should().BeTrue();
            saldo.SaldQuantidadeRestante.Should().Be(4);
        }

        [Fact]
        public void Ativar_DuasVezes_SegundaRetornaFalse()
        {
            var (_, saldo) = Novo();
            saldo.Ativar().Should().BeTrue();
            saldo.Ativar().Should().BeFalse();
        }

        [Fact]
        public void Ativar_Cancelado_RetornaFalse()
        {
            var (_, saldo) = Novo();
            saldo.Cancelar();
            saldo.Ativar().Should().BeFalse();
            saldo.SaldStatus.Should().Be(StatusSaldoPacote.Cancelado);
        }

        [Fact]
        public void Debitar_AteEsgotar_RetornaFalseDepois()
        {
            var (_, saldo) = Novo(qtd: 2);
            saldo.Ativar();
            saldo.Debitar().Should().BeTrue();
            saldo.Debitar().Should().BeTrue();
            saldo.Debitar().Should().BeFalse(); // saldo zerado
        }

        [Fact]
        public void DefinirGatewayId_PersisteValor()
        {
            var (_, saldo) = Novo();
            saldo.DefinirGatewayId("mp-12345");
            saldo.SaldGatewayPagamentoId.Should().Be("mp-12345");
        }
    }
}

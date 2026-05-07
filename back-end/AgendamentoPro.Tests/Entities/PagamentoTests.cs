using AgendamentoPro.Core.Entities.Pagamentos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class PagamentoTests
    {
        private static Pagamento Novo(decimal valor = 50m)
            => new(rTenId: 1, rAgeId: 100, FormaPagamento.Pix, valor, "MercadoPago",
                   DateTime.UtcNow.AddMinutes(15));

        [Fact]
        public void Construtor_DeveLancarSeValorNaoPositivo()
        {
            Action act = () => new Pagamento(1, 100, FormaPagamento.Pix, 0m, "MP", null);
            act.Should().Throw<DomainException>().WithMessage("*positivo*");
        }

        [Fact]
        public void Aprovar_PrimeiraVez_DeveRetornarTrueEAtualizarStatus()
        {
            var p = Novo();
            p.Aprovar("payload-1").Should().BeTrue();
            p.PagStatus.Should().Be(StatusPagamento.Aprovado);
            p.PagAprovadoEm.Should().NotBeNull();
        }

        [Fact]
        public void Aprovar_SegundaVez_DeveRetornarFalseEPreservarTimestamp()
        {
            var p = Novo();
            p.Aprovar("primeiro").Should().BeTrue();
            var primeiroAprovadoEm = p.PagAprovadoEm;

            p.Aprovar("segundo").Should().BeFalse();
            p.PagAprovadoEm.Should().Be(primeiroAprovadoEm);
        }

        [Fact]
        public void Recusar_Estornar_Expirar_SaoIdempotentes()
        {
            var p1 = Novo(); p1.Recusar().Should().BeTrue(); p1.Recusar().Should().BeFalse();
            var p2 = Novo(); p2.Estornar().Should().BeTrue(); p2.Estornar().Should().BeFalse();
            var p3 = Novo(); p3.Expirar().Should().BeTrue(); p3.Expirar().Should().BeFalse();
        }

        [Fact]
        public void DefinirDadosGateway_PreservaInformacoes()
        {
            var p = Novo();
            p.DefinirDadosGateway("gw-123", "qr-abc", "https://mp/x", "{\"raw\":1}");
            p.PagGatewayId.Should().Be("gw-123");
            p.PagQrCode.Should().Be("qr-abc");
            p.PagLinkPagamento.Should().Be("https://mp/x");
            p.PagPayloadGateway.Should().Be("{\"raw\":1}");
        }
    }
}

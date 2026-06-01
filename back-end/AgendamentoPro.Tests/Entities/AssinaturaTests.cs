using AgendamentoPro.Core.Entities.Assinaturas;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class AssinaturaTests
    {
        private static Assinatura Nova(DateTime? trial = null)
            => new(rTenId: 1, rPlnId: 1, gateway: "MercadoPago", trialAteEm: trial);

        [Fact]
        public void Construtor_SemTrial_StatusAtivo()
        {
            var a = Nova();
            a.AssStatus.Should().Be(StatusAssinatura.Ativa);
            a.AssTrialAteEm.Should().BeNull();
        }

        [Fact]
        public void Construtor_ComTrial_StatusTrial()
        {
            var a = Nova(DateTime.UtcNow.AddDays(14));
            a.AssStatus.Should().Be(StatusAssinatura.Trial);
        }

        [Fact]
        public void Construtor_ParametrosInvalidos_Lanca()
        {
            ((Action)(() => new Assinatura(0, 1, "MP"))).Should().Throw<DomainException>();
            ((Action)(() => new Assinatura(1, 0, "MP"))).Should().Throw<DomainException>();
            ((Action)(() => new Assinatura(1, 1, ""))).Should().Throw<DomainException>();
        }

        [Fact]
        public void MarcarAtrasada_PrimeiraVez_DeveTransicionar()
        {
            var a = Nova();
            a.MarcarAtrasada(DateTime.UtcNow).Should().BeTrue();
            a.AssStatus.Should().Be(StatusAssinatura.Atrasada);
            a.AssAtrasoDesde.Should().NotBeNull();
        }

        [Fact]
        public void MarcarAtrasada_Idempotente()
        {
            var a = Nova();
            a.MarcarAtrasada(DateTime.UtcNow);
            var primeiroDesde = a.AssAtrasoDesde;
            a.MarcarAtrasada(DateTime.UtcNow.AddDays(1)).Should().BeFalse();
            a.AssAtrasoDesde.Should().Be(primeiroDesde);
        }

        [Fact]
        public void TransicionarReadOnly_SoFuncionaSeAtrasada()
        {
            var a = Nova(); // Ativa
            a.TransicionarReadOnly(DateTime.UtcNow).Should().BeFalse();
            a.AssStatus.Should().Be(StatusAssinatura.Ativa);

            a.MarcarAtrasada(DateTime.UtcNow);
            a.TransicionarReadOnly(DateTime.UtcNow).Should().BeTrue();
            a.AssStatus.Should().Be(StatusAssinatura.ReadOnly);
        }

        [Fact]
        public void Expirar_SoFuncionaSeReadOnly()
        {
            var a = Nova();
            a.Expirar(DateTime.UtcNow).Should().BeFalse();
            a.MarcarAtrasada(DateTime.UtcNow);
            a.Expirar(DateTime.UtcNow).Should().BeFalse();
            a.TransicionarReadOnly(DateTime.UtcNow);
            a.Expirar(DateTime.UtcNow).Should().BeTrue();
            a.AssStatus.Should().Be(StatusAssinatura.Expirada);
        }

        [Fact]
        public void RegistrarPagamento_LimpaEstadoDeAtraso()
        {
            var a = Nova();
            a.MarcarAtrasada(DateTime.UtcNow);
            a.AssAtrasoDesde.Should().NotBeNull();

            var quando = DateTime.UtcNow;
            a.RegistrarPagamento(quando, quando.AddDays(30)).Should().BeTrue();
            a.AssStatus.Should().Be(StatusAssinatura.Ativa);
            a.AssAtrasoDesde.Should().BeNull();
            a.AssUltimoPagamentoEm.Should().Be(quando);
        }

        [Fact]
        public void Cancelar_BloqueiaTransicoesSeguintes()
        {
            var a = Nova();
            a.Cancelar(DateTime.UtcNow).Should().BeTrue();
            a.AssStatus.Should().Be(StatusAssinatura.Cancelada);
            a.MarcarAtrasada(DateTime.UtcNow).Should().BeFalse();
            a.TransicionarReadOnly(DateTime.UtcNow).Should().BeFalse();
        }

        [Fact]
        public void PermiteEscrita_AtivaAtrasadaTrial_True_ReadOnlyCancelada_False()
        {
            var a = Nova();
            a.PermiteEscrita().Should().BeTrue();
            a.MarcarAtrasada(DateTime.UtcNow);
            a.PermiteEscrita().Should().BeTrue();
            a.TransicionarReadOnly(DateTime.UtcNow);
            a.PermiteEscrita().Should().BeFalse();
            a.Expirar(DateTime.UtcNow);
            a.PermiteEscrita().Should().BeFalse();

            var c = Nova();
            c.Cancelar(DateTime.UtcNow);
            c.PermiteEscrita().Should().BeFalse();
        }

        [Fact]
        public void AlterarPlano_AtualizaIdEretornaTrueQuandoMuda()
        {
            var a = Nova();
            a.AlterarPlano(2).Should().BeTrue();
            a.R_PlnId.Should().Be(2);
            a.AlterarPlano(2).Should().BeFalse();
            ((Action)(() => a.AlterarPlano(0))).Should().Throw<DomainException>();
        }
    }

    public class PlanoTests
    {
        [Fact]
        public void Construtor_PrecoNaoPositivo_Lanca()
        {
            ((Action)(() => new Plano("X", "desc", 0m, 1, 1, -1)))
                .Should().Throw<DomainException>();
        }

        [Fact]
        public void Construtor_LimiteZero_Lanca()
        {
            ((Action)(() => new Plano("X", "desc", 10m, 0, 1, -1)))
                .Should().Throw<DomainException>();
            ((Action)(() => new Plano("X", "desc", 10m, 1, 0, -1)))
                .Should().Throw<DomainException>();
        }

        [Fact]
        public void Construtor_LimiteIlimitado_OK()
        {
            var p = new Plano("Pro", "ilim", 79.90m, -1, -1, -1);
            p.PlnLimiteUnidades.Should().Be(-1);
        }

        [Fact]
        public void RespeitaLimites_IlimitadoSempreTrue()
        {
            var p = new Plano("Pro", "ilim", 79.90m, -1, -1, -1);
            p.RespeitaLimiteUnidades(int.MaxValue).Should().BeTrue();
            p.RespeitaLimiteProfissionais(int.MaxValue).Should().BeTrue();
            p.RespeitaLimiteAgendamentos(int.MaxValue).Should().BeTrue();
        }

        [Fact]
        public void RespeitaLimiteProfissionais_AcimaDoTeto_False()
        {
            var p = new Plano("Basico", "", 29.90m, 1, 10, -1);
            p.RespeitaLimiteProfissionais(9).Should().BeTrue();
            p.RespeitaLimiteProfissionais(10).Should().BeFalse();
            p.RespeitaLimiteProfissionais(99).Should().BeFalse();
        }
    }

    public class FaturaAssinaturaTests
    {
        private static FaturaAssinatura Nova(decimal valor = 29.90m)
        {
            var ini = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var fim = ini.AddMonths(1);
            return new FaturaAssinatura(rTenId: 1, rAssId: 1, valor, ini, fim, fim);
        }

        [Fact]
        public void Construtor_ValorNaoPositivo_Lanca()
        {
            var ini = DateTime.UtcNow; var fim = ini.AddMonths(1);
            ((Action)(() => new FaturaAssinatura(1, 1, 0m, ini, fim, fim)))
                .Should().Throw<DomainException>();
        }

        [Fact]
        public void Construtor_PeriodoInvertido_Lanca()
        {
            var ini = DateTime.UtcNow;
            ((Action)(() => new FaturaAssinatura(1, 1, 10m, ini, ini.AddDays(-1), ini)))
                .Should().Throw<DomainException>();
        }

        [Fact]
        public void Pagar_Recusar_Estornar_SaoIdempotentes()
        {
            var f1 = Nova(); f1.Pagar(DateTime.UtcNow).Should().BeTrue(); f1.Pagar(DateTime.UtcNow).Should().BeFalse();
            var f2 = Nova(); f2.Recusar().Should().BeTrue(); f2.Recusar().Should().BeFalse();
            var f3 = Nova(); f3.Estornar().Should().BeTrue(); f3.Estornar().Should().BeFalse();
        }
    }
}

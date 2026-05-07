using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Exceptions;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class PontosFidelidadeTests
    {
        [Fact]
        public void Construtor_NasceCom0()
        {
            var p = new PontosFidelidade(1, 7);
            p.PtsSaldo.Should().Be(0);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(0, 0)]
        public void Construtor_TenantOuClienteZero_DeveLancar(int ten, int cli)
        {
            Action act = () => new PontosFidelidade(ten, cli);
            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void Creditar_ValorPositivo_AumentaSaldo()
        {
            var p = new PontosFidelidade(1, 1);
            p.Creditar(10);
            p.PtsSaldo.Should().Be(10);
            p.Creditar(15);
            p.PtsSaldo.Should().Be(25);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Creditar_ValorInvalido_NaoAlteraSaldo(int valor)
        {
            var p = new PontosFidelidade(1, 1);
            p.Creditar(50);
            p.Creditar(valor);
            p.PtsSaldo.Should().Be(50);
        }

        [Fact]
        public void Debitar_ComSaldo_RetornaTrueEReduz()
        {
            var p = new PontosFidelidade(1, 1);
            p.Creditar(100);
            p.Debitar(30).Should().BeTrue();
            p.PtsSaldo.Should().Be(70);
        }

        [Fact]
        public void Debitar_AcimaDoSaldo_RetornaFalseSemAlterar()
        {
            var p = new PontosFidelidade(1, 1);
            p.Creditar(100);
            p.Debitar(150).Should().BeFalse();
            p.PtsSaldo.Should().Be(100);
        }

        [Fact]
        public void Debitar_ValorZeroOuNegativo_RetornaFalse()
        {
            var p = new PontosFidelidade(1, 1);
            p.Creditar(50);
            p.Debitar(0).Should().BeFalse();
            p.Debitar(-10).Should().BeFalse();
            p.PtsSaldo.Should().Be(50);
        }
    }
}

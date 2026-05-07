using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Exceptions;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class CupomTests
    {
        private static Cupom Novo(TipoDesconto tipo = TipoDesconto.Percentual, decimal valor = 10m,
            DateTime? validoAte = null, int usosMax = 0)
        {
            return new Cupom(rTenId: 1, codigo: "ABC10", tipo: tipo, valor: valor,
                validoDe: DateTime.UtcNow.AddDays(-1),
                validoAte: validoAte ?? DateTime.UtcNow.AddDays(30),
                usosMaximos: usosMax);
        }

        [Fact]
        public void Construtor_PercentualAcimaDe100_DeveLancar()
        {
            Action act = () => new Cupom(1, "X", TipoDesconto.Percentual, 150m,
                DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 0);
            act.Should().Throw<DomainException>().WithMessage("*100*");
        }

        [Fact]
        public void Construtor_DataFimAntesDataInicio_DeveLancar()
        {
            Action act = () => new Cupom(1, "X", TipoDesconto.Percentual, 10m,
                DateTime.UtcNow.AddDays(2), DateTime.UtcNow, 0);
            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void Construtor_CodigoEhUppercase()
        {
            var c = new Cupom(1, "abc-123", TipoDesconto.Percentual, 10m,
                DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 0);
            c.CupCodigo.Should().Be("ABC-123");
        }

        [Fact]
        public void EhValido_AtivoDentroJanelaSemUsos_True()
        {
            var c = Novo();
            c.EhValido(DateTime.UtcNow).Should().BeTrue();
        }

        [Fact]
        public void EhValido_Expirado_False()
        {
            // Cupom criado válido entre 10 dias atrás e 1 dia atrás → hoje já está expirado
            var c = new Cupom(rTenId: 1, codigo: "X", tipo: TipoDesconto.Percentual, valor: 10m,
                validoDe: DateTime.UtcNow.AddDays(-10),
                validoAte: DateTime.UtcNow.AddDays(-1),
                usosMaximos: 0);
            c.EhValido(DateTime.UtcNow).Should().BeFalse();
        }

        [Fact]
        public void EhValido_Inativo_False()
        {
            var c = Novo();
            c.Desativar();
            c.EhValido(DateTime.UtcNow).Should().BeFalse();
        }

        [Fact]
        public void EhValido_LimiteUsosAtingido_False()
        {
            var c = Novo(usosMax: 1);
            c.EhValido(DateTime.UtcNow).Should().BeTrue();
            c.RegistrarUso();
            c.EhValido(DateTime.UtcNow).Should().BeFalse();
        }

        [Fact]
        public void CalcularDesconto_Percentual_AplicaCorretamente()
        {
            var c = Novo(TipoDesconto.Percentual, 20m);
            c.CalcularDesconto(100m).Should().Be(80m);
            c.CalcularDesconto(50m).Should().Be(40m);
        }

        [Fact]
        public void CalcularDesconto_ValorFixo_NuncaNegativo()
        {
            var c = Novo(TipoDesconto.ValorFixo, 50m);
            c.CalcularDesconto(100m).Should().Be(50m);
            c.CalcularDesconto(30m).Should().Be(0m);
        }
    }
}

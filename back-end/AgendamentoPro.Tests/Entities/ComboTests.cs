using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Exceptions;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class ComboTests
    {
        [Fact]
        public void Construtor_DadosValidos_CriaCombo()
        {
            var c = new Combo(1, "Combo Premium", "desc", null, 100m, 0);
            c.ComNome.Should().Be("Combo Premium");
            c.ComPrecoPromocional.Should().Be(100m);
            c.ComAtivo.Should().BeTrue();
            c.Servicos.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Construtor_PrecoInvalido_DeveLancar(decimal preco)
        {
            Action act = () => new Combo(1, "x", null, null, preco, 0);
            act.Should().Throw<ServicoException>();
        }

        [Fact]
        public void Construtor_NomeVazio_DeveLancar()
        {
            Action act = () => new Combo(1, " ", null, null, 100m, 0);
            act.Should().Throw<ServicoException>();
        }

        [Fact]
        public void DefinirServicos_RemoveDuplicatasMantemUnicos()
        {
            var c = new Combo(1, "x", null, null, 100m, 0);
            c.DefinirServicos(new[] { 1, 2, 2, 3, 1 });
            c.Servicos.Should().HaveCount(3);
            c.Servicos.Select(s => s.R_SerId).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }

        [Fact]
        public void DefinirServicos_ChamadoDuasVezes_SubstituiLista()
        {
            var c = new Combo(1, "x", null, null, 100m, 0);
            c.DefinirServicos(new[] { 1, 2 });
            c.DefinirServicos(new[] { 3, 4, 5 });
            c.Servicos.Select(s => s.R_SerId).Should().BeEquivalentTo(new[] { 3, 4, 5 });
        }

        [Fact]
        public void Atualizar_AlteraTodosOsCamposEValida()
        {
            var c = new Combo(1, "x", null, null, 100m, 0);
            c.Atualizar("Novo nome", "nova desc", "http://img", 80m, 5, false);
            c.ComNome.Should().Be("Novo nome");
            c.ComPrecoPromocional.Should().Be(80m);
            c.ComAtivo.Should().BeFalse();
            c.ComOrdem.Should().Be(5);
        }
    }
}

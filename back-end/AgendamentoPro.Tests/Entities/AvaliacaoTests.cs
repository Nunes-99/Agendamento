using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Exceptions;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class AvaliacaoTests
    {
        [Fact]
        public void Construtor_GeraTokenValido_NaoExpiradaPorDefault()
        {
            var a = new Avaliacao(1, 100, 50);
            a.AvaToken.Should().NotBe(Guid.Empty);
            a.AvaPublica.Should().BeTrue();
            a.AvaRespondidoEm.Should().BeNull();
            a.AvaNota.Should().BeNull();
        }

        [Theory]
        [InlineData(0, 1, 1)]
        [InlineData(1, 0, 1)]
        [InlineData(1, 1, 0)]
        public void Construtor_IdInvalido_DeveLancar(int ten, int age, int cli)
        {
            Action act = () => new Avaliacao(ten, age, cli);
            act.Should().Throw<DomainException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        public void Responder_NotaForaDoIntervalo_DeveLancar(int nota)
        {
            var a = new Avaliacao(1, 100, 50);
            Action act = () => a.Responder(nota, "ok");
            act.Should().Throw<DomainException>().WithMessage("*1 e 5*");
        }

        [Fact]
        public void Responder_DuasVezes_DeveLancarNaSegunda()
        {
            var a = new Avaliacao(1, 100, 50);
            a.Responder(5, "primeiro");
            Action act = () => a.Responder(4, "segundo");
            act.Should().Throw<DomainException>().WithMessage("*já foi respondida*");
        }

        [Fact]
        public void Responder_TruncaComentariosLongos()
        {
            var a = new Avaliacao(1, 100, 50);
            a.Responder(5, new string('x', 2000));
            a.AvaComentario!.Length.Should().Be(1000);
        }

        [Fact]
        public void DefinirVisibilidade_AlternaCampoPublica()
        {
            var a = new Avaliacao(1, 100, 50);
            a.DefinirVisibilidade(false);
            a.AvaPublica.Should().BeFalse();
            a.DefinirVisibilidade(true);
            a.AvaPublica.Should().BeTrue();
        }
    }
}

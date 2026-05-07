using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Exceptions;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class PasswordResetTests
    {
        [Fact]
        public void Construtor_DadosValidos_CriaTokenNaoUsadoComExpiracao()
        {
            var r = new PasswordReset(rUsuId: 7, token: "abc-123", validade: TimeSpan.FromHours(1));
            r.R_UsuId.Should().Be(7);
            r.RpsToken.Should().Be("abc-123");
            r.RpsUsado.Should().BeFalse();
            r.RpsExpiraEm.Should().BeAfter(DateTime.UtcNow);
            r.RpsExpiraEm.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(2));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Construtor_UsuarioInvalido_DeveLancar(int usuId)
        {
            Action act = () => new PasswordReset(usuId, "tok", TimeSpan.FromHours(1));
            act.Should().Throw<DomainException>().WithMessage("*Usuário*");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Construtor_TokenVazio_DeveLancar(string token)
        {
            Action act = () => new PasswordReset(1, token, TimeSpan.FromHours(1));
            act.Should().Throw<DomainException>().WithMessage("*Token*");
        }

        [Fact]
        public void EstaValido_NaoUsadoENaoExpirado_RetornaTrue()
        {
            var r = new PasswordReset(1, "x", TimeSpan.FromHours(1));
            r.EstaValido(DateTime.UtcNow).Should().BeTrue();
        }

        [Fact]
        public void EstaValido_Expirado_RetornaFalse()
        {
            var r = new PasswordReset(1, "x", TimeSpan.FromHours(1));
            r.EstaValido(DateTime.UtcNow.AddHours(2)).Should().BeFalse();
        }

        [Fact]
        public void EstaValido_Usado_RetornaFalse()
        {
            var r = new PasswordReset(1, "x", TimeSpan.FromHours(1));
            r.MarcarUsado();
            r.EstaValido(DateTime.UtcNow).Should().BeFalse();
        }

        [Fact]
        public void MarcarUsado_DuasVezes_DeveLancar()
        {
            var r = new PasswordReset(1, "x", TimeSpan.FromHours(1));
            r.MarcarUsado();
            Action act = () => r.MarcarUsado();
            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void MarcarUsado_RegistraTimestamp()
        {
            var r = new PasswordReset(1, "x", TimeSpan.FromHours(1));
            r.RpsUsadoEm.Should().BeNull();
            r.MarcarUsado();
            r.RpsUsadoEm.Should().NotBeNull();
            r.RpsUsadoEm!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }
    }
}

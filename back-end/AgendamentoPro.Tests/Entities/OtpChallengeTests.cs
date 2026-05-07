using AgendamentoPro.Core.Entities.Usuarios;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class OtpChallengeTests
    {
        private static OtpChallenge Novo() =>
            new OtpChallenge(rTenId: 1, telefone: "11999999999",
                codigoHash: "$2a$11$hash", validade: TimeSpan.FromMinutes(10));

        [Fact]
        public void Construtor_NormalizaTelefoneTrim()
        {
            var c = new OtpChallenge(1, "  11999999999  ", "h", TimeSpan.FromMinutes(10));
            c.OtpTelefone.Should().Be("11999999999");
        }

        [Fact]
        public void Disponivel_NovoChallenge_True()
        {
            var c = Novo();
            c.Disponivel(DateTime.UtcNow).Should().BeTrue();
        }

        [Fact]
        public void Disponivel_AposExpirar_False()
        {
            var c = Novo();
            c.Disponivel(DateTime.UtcNow.AddMinutes(11)).Should().BeFalse();
        }

        [Fact]
        public void Expirou_AntesDoTempo_False()
        {
            var c = Novo();
            c.Expirou(DateTime.UtcNow).Should().BeFalse();
        }

        [Fact]
        public void Expirou_DepoisDoTempo_True()
        {
            var c = Novo();
            c.Expirou(DateTime.UtcNow.AddMinutes(11)).Should().BeTrue();
        }

        [Fact]
        public void RegistrarFalha_3Vezes_BloqueiaPorExcessoTentativas()
        {
            var c = Novo();
            c.RegistrarFalha();
            c.RegistrarFalha();
            c.RegistrarFalha();
            c.ExcedeuTentativas().Should().BeTrue();
            c.Disponivel(DateTime.UtcNow).Should().BeFalse();
            c.OtpTentativas.Should().Be(3);
        }

        [Fact]
        public void RegistrarFalha_2Vezes_AindaDisponivel()
        {
            var c = Novo();
            c.RegistrarFalha();
            c.RegistrarFalha();
            c.Disponivel(DateTime.UtcNow).Should().BeTrue();
        }

        [Fact]
        public void MarcarUsado_TornaIndisponivel()
        {
            var c = Novo();
            c.MarcarUsado();
            c.OtpUsado.Should().BeTrue();
            c.Disponivel(DateTime.UtcNow).Should().BeFalse();
        }
    }
}

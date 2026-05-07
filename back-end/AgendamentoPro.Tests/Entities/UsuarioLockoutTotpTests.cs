using AgendamentoPro.Core.Entities.Usuarios;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class UsuarioLockoutTotpTests
    {
        private static Usuario Novo() =>
            new(rTenId: 1, nome: "Admin", email: "x@y.com", senhaHash: "h",
                perfil: "Administrador", telefone: null);

        [Fact]
        public void Falhas_AbaixoLimite_NaoBloqueia()
        {
            var u = Novo();
            for (int i = 0; i < 4; i++)
                u.RegistrarFalhaLogin(tentativasMax: 5, duracaoBloqueio: TimeSpan.FromMinutes(15));
            u.UsuTentativasFalhas.Should().Be(4);
            u.EstaBloqueado(DateTime.UtcNow).Should().BeFalse();
        }

        [Fact]
        public void Falhas_AtingeLimite_Bloqueia()
        {
            var u = Novo();
            for (int i = 0; i < 5; i++)
                u.RegistrarFalhaLogin(5, TimeSpan.FromMinutes(15));
            u.EstaBloqueado(DateTime.UtcNow).Should().BeTrue();
            u.UsuBloqueadoAte.Should().NotBeNull();
        }

        [Fact]
        public void RegistrarLogin_LimpaTentativasEBloqueio()
        {
            var u = Novo();
            for (int i = 0; i < 5; i++) u.RegistrarFalhaLogin(5, TimeSpan.FromMinutes(15));
            u.RegistrarLogin();
            u.UsuTentativasFalhas.Should().Be(0);
            u.UsuBloqueadoAte.Should().BeNull();
        }

        [Fact]
        public void AlterarSenha_LimpaTentativasEBloqueio()
        {
            var u = Novo();
            for (int i = 0; i < 5; i++) u.RegistrarFalhaLogin(5, TimeSpan.FromMinutes(15));
            u.AlterarSenha("novo-hash");
            u.UsuTentativasFalhas.Should().Be(0);
            u.UsuBloqueadoAte.Should().BeNull();
        }

        [Fact]
        public void EstaBloqueado_DepoisDaJanela_VoltaFalse()
        {
            var u = Novo();
            for (int i = 0; i < 5; i++) u.RegistrarFalhaLogin(5, TimeSpan.FromMinutes(15));
            u.EstaBloqueado(DateTime.UtcNow.AddMinutes(20)).Should().BeFalse();
        }

        [Fact]
        public void DefinirTotpSecret_SozinhoNaoAtiva()
        {
            var u = Novo();
            u.DefinirTotpSecret("BASE32SECRET");
            u.UsuTotpSecret.Should().Be("BASE32SECRET");
            u.UsuTotpAtivo.Should().BeFalse(); // pendente até confirmar
        }

        [Fact]
        public void AtivarTotp_DepoisDeDefinirSecret_LigaFlag()
        {
            var u = Novo();
            u.DefinirTotpSecret("BASE32SECRET");
            u.AtivarTotp();
            u.UsuTotpAtivo.Should().BeTrue();
        }

        [Fact]
        public void AtivarTotp_SemSecret_NaoAtiva()
        {
            var u = Novo();
            u.AtivarTotp();
            u.UsuTotpAtivo.Should().BeFalse();
        }

        [Fact]
        public void DesativarTotp_LimpaSecretEFlag()
        {
            var u = Novo();
            u.DefinirTotpSecret("X");
            u.AtivarTotp();
            u.DesativarTotp();
            u.UsuTotpSecret.Should().BeNull();
            u.UsuTotpAtivo.Should().BeFalse();
        }
    }
}

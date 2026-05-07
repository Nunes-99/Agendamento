using AgendamentoPro.Application.InputModels.Auth;
using AgendamentoPro.Application.UseCases.Auth;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AgendamentoPro.Tests.UseCases
{
    public class OtpUseCaseTests
    {
        private readonly Mock<IOtpChallengeRepository> _otps = new();
        private readonly Mock<IClienteRepository> _clientes = new();
        private readonly Mock<INotificadorWhatsApp> _whats = new();
        private readonly Mock<ITokenService> _token = new();
        private readonly Mock<IPasswordHasher> _hasher = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        private OtpUseCase Criar()
        {
            _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(s => $"hash:{s}");
            _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((codigo, hash) => hash == $"hash:{codigo}");
            return new OtpUseCase(_otps.Object, _clientes.Object, _whats.Object,
                _token.Object, _hasher.Object, _uow.Object, NullLogger<OtpUseCase>.Instance);
        }

        [Fact]
        public async Task Solicitar_TelefoneInvalido_NaoEnvia()
        {
            var uc = Criar();
            var r = await uc.SolicitarAsync(1, "salao", new SolicitarOtpInputModel { Telefone = "abc" });
            r.Enviado.Should().BeFalse();
            _otps.Verify(o => o.CreateAsync(It.IsAny<OtpChallenge>()), Times.Never);
        }

        [Fact]
        public async Task Solicitar_AcimaDoLimiteHora_NaoEnvia()
        {
            _otps.Setup(o => o.ContarRecentesAsync(1, "11999999999", It.IsAny<DateTime>()))
                .ReturnsAsync(5); // limite = 5
            var uc = Criar();
            var r = await uc.SolicitarAsync(1, "salao", new SolicitarOtpInputModel { Telefone = "11999999999" });
            r.Enviado.Should().BeFalse();
        }

        [Fact]
        public async Task Solicitar_DentroDoCooldown_NaoEnvia()
        {
            _otps.Setup(o => o.ContarRecentesAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .ReturnsAsync(0);
            var ultimo = new OtpChallenge(1, "11999999999", "h", TimeSpan.FromMinutes(10));
            _otps.Setup(o => o.GetUltimoAtivoAsync(1, "11999999999")).ReturnsAsync(ultimo);
            var uc = Criar();
            var r = await uc.SolicitarAsync(1, "salao", new SolicitarOtpInputModel { Telefone = "11999999999" });
            r.Enviado.Should().BeFalse();
            r.CooldownSegundos.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Solicitar_DentroLimites_CriaChallenge()
        {
            _otps.Setup(o => o.ContarRecentesAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .ReturnsAsync(0);
            _otps.Setup(o => o.GetUltimoAtivoAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((OtpChallenge)null);
            _whats.SetupGet(w => w.Ativo).Returns(false); // modo dev silencioso

            var uc = Criar();
            var r = await uc.SolicitarAsync(1, "salao", new SolicitarOtpInputModel { Telefone = "(11) 99999-9999" });

            r.Enviado.Should().BeTrue();
            r.ExpiraEm.Should().BeAfter(DateTime.UtcNow.AddMinutes(5));
            _otps.Verify(o => o.CreateAsync(It.IsAny<OtpChallenge>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Validar_TelefoneOuCodigoInvalido_RetornaInvalido()
        {
            var uc = Criar();
            var r1 = await uc.ValidarAsync(1, "salao", new ValidarOtpInputModel { Telefone = "", Codigo = "123456" });
            r1.Valido.Should().BeFalse();
            var r2 = await uc.ValidarAsync(1, "salao", new ValidarOtpInputModel { Telefone = "11999999999", Codigo = "12" });
            r2.Valido.Should().BeFalse();
        }

        [Fact]
        public async Task Validar_SemChallenge_RetornaInvalido()
        {
            _otps.Setup(o => o.GetUltimoAtivoAsync(1, "11999999999")).ReturnsAsync((OtpChallenge)null);
            var uc = Criar();
            var r = await uc.ValidarAsync(1, "salao",
                new ValidarOtpInputModel { Telefone = "11999999999", Codigo = "123456" });
            r.Valido.Should().BeFalse();
            r.Mensagem.Should().Contain("expirado").And.Contain("solicitado");
        }

        [Fact]
        public async Task Validar_CodigoIncorreto_RegistraFalhaENaoEmiteToken()
        {
            var ch = new OtpChallenge(1, "11999999999", "hash:111111", TimeSpan.FromMinutes(10));
            _otps.Setup(o => o.GetUltimoAtivoAsync(1, "11999999999")).ReturnsAsync(ch);
            var uc = Criar();
            var r = await uc.ValidarAsync(1, "salao",
                new ValidarOtpInputModel { Telefone = "11999999999", Codigo = "999999" });
            r.Valido.Should().BeFalse();
            ch.OtpTentativas.Should().Be(1);
            _token.Verify(t => t.GerarTokenCliente(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Validar_CodigoCorreto_GeraTokenEMarcaUsado()
        {
            var ch = new OtpChallenge(1, "11999999999", "hash:111111", TimeSpan.FromMinutes(10));
            _otps.Setup(o => o.GetUltimoAtivoAsync(1, "11999999999")).ReturnsAsync(ch);
            var cliente = new Cliente(1, "Vitor", null, "11999999999", "11999999999", null);
            _clientes.Setup(c => c.GetByTelefoneAsync(1, "11999999999")).ReturnsAsync(cliente);
            _token.Setup(t => t.GerarTokenCliente(It.IsAny<int>(), 1, "salao"))
                .Returns(("jwt-fake", DateTime.UtcNow.AddDays(7)));

            var uc = Criar();
            var r = await uc.ValidarAsync(1, "salao",
                new ValidarOtpInputModel { Telefone = "11999999999", Codigo = "111111" });

            r.Valido.Should().BeTrue();
            r.Token.Should().Be("jwt-fake");
            r.ClienteNome.Should().Be("Vitor");
            ch.OtpUsado.Should().BeTrue();
        }

        [Fact]
        public async Task Validar_ClienteNaoExiste_CriaCliente()
        {
            var ch = new OtpChallenge(1, "11999999999", "hash:111111", TimeSpan.FromMinutes(10));
            _otps.Setup(o => o.GetUltimoAtivoAsync(1, "11999999999")).ReturnsAsync(ch);
            _clientes.Setup(c => c.GetByTelefoneAsync(1, "11999999999")).ReturnsAsync((Cliente)null);
            _token.Setup(t => t.GerarTokenCliente(It.IsAny<int>(), 1, "salao"))
                .Returns(("jwt-fake", DateTime.UtcNow.AddDays(7)));

            var uc = Criar();
            var r = await uc.ValidarAsync(1, "salao",
                new ValidarOtpInputModel { Telefone = "11999999999", Codigo = "111111" });

            r.Valido.Should().BeTrue();
            _clientes.Verify(c => c.CreateAsync(It.IsAny<Cliente>()), Times.Once);
        }
    }
}

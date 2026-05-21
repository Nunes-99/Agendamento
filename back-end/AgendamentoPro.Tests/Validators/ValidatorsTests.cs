using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Application.InputModels.Auth;
using AgendamentoPro.Application.InputModels.Servicos;
using AgendamentoPro.Application.InputModels.Tenants;
using AgendamentoPro.Application.Validators.Agendamentos;
using AgendamentoPro.Application.Validators.Auth;
using AgendamentoPro.Application.Validators.Servicos;
using AgendamentoPro.Application.Validators.Tenants;
using FluentAssertions;

namespace AgendamentoPro.Tests.Validators
{
    public class ValidatorsTests
    {
        [Fact]
        public void LoginValidator_EmailVazio_FailValidation()
        {
            var v = new LoginValidator();
            var r = v.Validate(new LoginInputModel { Email = "", Senha = "abcdef" });
            r.IsValid.Should().BeFalse();
            r.Errors.Should().Contain(e => e.PropertyName == nameof(LoginInputModel.Email));
        }

        [Fact]
        public void LoginValidator_EmailValido_SenhaCurta_FailValidation()
        {
            var v = new LoginValidator();
            var r = v.Validate(new LoginInputModel { Email = "x@y.com", Senha = "abc" });
            r.IsValid.Should().BeFalse();
            r.Errors.Should().Contain(e => e.PropertyName == nameof(LoginInputModel.Senha));
        }

        [Fact]
        public void LoginValidator_EntradaValida_PassaValidation()
        {
            var v = new LoginValidator();
            var r = v.Validate(new LoginInputModel { Email = "x@y.com", Senha = "secret123" });
            r.IsValid.Should().BeTrue();
        }

        [Fact]
        public void CriarAgendamentoValidator_DataPassada_FailValidation()
        {
            var v = new CriarAgendamentoValidator();
            var r = v.Validate(new CriarAgendamentoInputModel
            {
                ServicoId = 1,
                Data = DateTime.UtcNow.AddDays(-1),
                HoraInicio = TimeSpan.FromHours(10),
                Cliente = new ClientePublicoInputModel { Nome = "X", Telefone = "11999999999" }
            });
            r.IsValid.Should().BeFalse();
        }

        [Fact]
        public void CriarAgendamentoValidator_SemContato_FailValidation()
        {
            var v = new CriarAgendamentoValidator();
            var r = v.Validate(new CriarAgendamentoInputModel
            {
                ServicoId = 1,
                Data = DateTime.UtcNow.AddDays(2),
                HoraInicio = TimeSpan.FromHours(10),
                Cliente = new ClientePublicoInputModel { Nome = "X" }
            });
            r.IsValid.Should().BeFalse();
        }

        [Fact]
        public void ServicoValidator_PrecoZero_FailValidation()
        {
            var v = new ServicoValidator();
            var r = v.Validate(new ServicoInputModel { Nome = "X", Preco = 0m, DuracaoMinutos = 30 });
            r.IsValid.Should().BeFalse();
        }

        [Fact]
        public void ServicoValidator_DuracaoMaiorQue24h_FailValidation()
        {
            var v = new ServicoValidator();
            var r = v.Validate(new ServicoInputModel { Nome = "X", Preco = 50m, DuracaoMinutos = 24 * 60 + 1 });
            r.IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("a")]                       // muito curto, mas passa min de 1 char
        [InlineData("with-dash")]
        [InlineData("123")]
        public void CriarTenantValidator_SlugsValidos_Passam(string slug)
        {
            var v = new CriarTenantValidator();
            var input = new CriarTenantInputModel
            {
                Nome = "X",
                Slug = slug,
                Email = "x@y.com",
                AdminNome = "A",
                AdminEmail = "a@a.com",
                AdminSenha = "12345678"
            };
            var r = v.Validate(input);
            r.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("-comeca-com-hifen")]
        [InlineData("termina-com-hifen-")]
        [InlineData("CAIXAALTA")]
        [InlineData("com_underscore")]
        public void CriarTenantValidator_SlugsInvalidos_Falham(string slug)
        {
            var v = new CriarTenantValidator();
            var input = new CriarTenantInputModel
            {
                Nome = "X", Slug = slug, Email = "x@y.com",
                AdminNome = "A", AdminEmail = "a@a.com", AdminSenha = "12345678"
            };
            var r = v.Validate(input);
            r.IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("https://cdn.example.com/logo.png")]
        [InlineData("http://example.com/x.jpg")]
        [InlineData("/uploads/1/2/foto.jpg")]
        [InlineData("")]
        [InlineData(null)]
        public void PersonalizacaoValidator_UrlsSeguras_Passa(string url)
        {
            var v = new AtualizarPersonalizacaoValidator();
            var r = v.Validate(new AtualizarPersonalizacaoInputModel
            {
                LogoUrl = url, BannerUrl = url, FaviconUrl = url
            });
            r.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
        [InlineData("file:///etc/passwd")]
        [InlineData("vbscript:msgbox(1)")]
        [InlineData("nao-e-url-valida")]
        public void PersonalizacaoValidator_UrlsInseguras_Reprovadas(string url)
        {
            var v = new AtualizarPersonalizacaoValidator();
            var r = v.Validate(new AtualizarPersonalizacaoInputModel { LogoUrl = url });
            r.IsValid.Should().BeFalse();
            r.Errors.Should().Contain(e => e.PropertyName == nameof(AtualizarPersonalizacaoInputModel.LogoUrl));
        }
    }
}

using AgendamentoPro.Core.Enums;
using AgendamentoPro.Infrastructure.Services.Pagamento;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgendamentoPro.Tests.Services
{
    /// <summary>
    /// Cobre o comportamento offline do StripeGateway (sem chamar Stripe).
    /// Tests que exigem API real (criar Checkout Session, validar webhook
    /// com signature válida) ficam fora — exigem mock HTTP ou conta TEST.
    /// </summary>
    public class StripeGatewayTests : IDisposable
    {
        public StripeGatewayTests()
        {
            Environment.SetEnvironmentVariable("STRIPE_SECRET_KEY", null);
            Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_SECRET", null);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("STRIPE_SECRET_KEY", null);
            Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_SECRET", null);
        }

        private static StripeGateway Criar() => new(
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            new NullLogger<StripeGateway>());

        /// <summary>Gateway com credencial — é o que existe num tenant que cobra.</summary>
        private static StripeGateway CriarConfigurado() => new(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Stripe:SecretKey"] = "sk_test_chave_de_teste"
                })
                .Build(),
            new NullLogger<StripeGateway>());

        [Fact]
        public void Nome_RetornaStripe()
        {
            Criar().Nome.Should().Be("Stripe");
        }

        [Fact]
        public void Suporta_ComChave_ApenasCartoes()
        {
            var g = CriarConfigurado();
            g.Suporta(FormaPagamento.CartaoCredito).Should().BeTrue();
            g.Suporta(FormaPagamento.CartaoDebito).Should().BeTrue();
            g.Suporta(FormaPagamento.Pix).Should().BeFalse();
            g.Suporta(FormaPagamento.Boleto).Should().BeFalse();
            g.Suporta(FormaPagamento.Dinheiro).Should().BeFalse();
        }

        [Fact]
        public void Suporta_SemChave_NaoSeOferece_Para_Nada()
        {
            // Um gateway sem credencial que diz "suporto cartão" faz o pedido
            // chegar até ele e estourar 500 na hora de cobrar. Declarando-se
            // indisponível, a escolha do meio de pagamento falha antes, com erro
            // que o cliente entende.
            var g = Criar();
            g.Suporta(FormaPagamento.CartaoCredito).Should().BeFalse();
            g.Suporta(FormaPagamento.CartaoDebito).Should().BeFalse();
        }

        [Fact]
        public async Task CriarCobranca_SemSecretKey_Lanca()
        {
            var g = Criar();
            Func<Task> act = () => g.CriarCobrancaAsync(1, 1, 100m, FormaPagamento.CartaoCredito, "x", 30);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*STRIPE_SECRET_KEY*");
        }

        [Fact]
        public async Task CriarCobranca_FormaPix_LancaComMensagemClara()
        {
            Environment.SetEnvironmentVariable("STRIPE_SECRET_KEY", "sk_test_fake_for_test");
            var g = Criar();
            Func<Task> act = () => g.CriarCobrancaAsync(1, 1, 100m, FormaPagamento.Pix, "x", 30);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*PIX*MercadoPago*");
        }

        [Fact]
        public async Task ProcessarWebhook_SemSecret_Lanca()
        {
            Environment.SetEnvironmentVariable("STRIPE_SECRET_KEY", "sk_test_fake_for_test");
            var g = Criar();
            Func<Task> act = () => g.ProcessarWebhookAsync("{}", "sig");
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*STRIPE_WEBHOOK_SECRET*");
        }

        [Fact]
        public async Task ProcessarWebhook_AssinaturaInvalida_LancaUnauthorized()
        {
            Environment.SetEnvironmentVariable("STRIPE_SECRET_KEY", "sk_test_fake_for_test");
            Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_SECRET", "whsec_fake_for_test");
            var g = Criar();
            Func<Task> act = () => g.ProcessarWebhookAsync("{\"id\":\"evt_1\"}", "assinatura-invalida");
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }
    }
}

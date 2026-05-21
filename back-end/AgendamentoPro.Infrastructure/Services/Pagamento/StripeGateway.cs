using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace AgendamentoPro.Infrastructure.Services.Pagamento
{
    /// <summary>
    /// Gateway Stripe via Checkout Sessions (cartão de crédito/débito).
    ///
    /// <para>Cobertura:</para>
    /// <list type="bullet">
    /// <item>FormaPagamento.CartaoCredito / CartaoDebito → Checkout Session com URL hospedada Stripe</item>
    /// <item>FormaPagamento.Pix → não suportado (use MercadoPagoGateway pra PIX BR)</item>
    /// </list>
    ///
    /// <para>Variáveis de ambiente:</para>
    /// <list type="bullet">
    /// <item>STRIPE_SECRET_KEY — sk_live_... / sk_test_...</item>
    /// <item>STRIPE_WEBHOOK_SECRET — whsec_... (Stripe Dashboard → Developers → Webhooks)</item>
    /// <item>STRIPE_CURRENCY — default "brl"</item>
    /// <item>APP_PUBLIC_URL — base pra success/cancel redirect</item>
    /// </list>
    ///
    /// Webhook: POST /api/v1/webhooks/pagamento/Stripe com header Stripe-Signature.
    /// </summary>
    public class StripeGateway : IGatewayPagamento
    {
        private readonly ILogger<StripeGateway> _logger;
        private readonly string _secretKey;
        private readonly string _webhookSecret;
        private readonly string _currency;
        private readonly string _appPublicUrl;

        public string Nome => "Stripe";

        public StripeGateway(IConfiguration config, ILogger<StripeGateway> logger)
        {
            _logger = logger;
            _secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
                ?? config["Stripe:SecretKey"] ?? string.Empty;
            _webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
                ?? config["Stripe:WebhookSecret"] ?? string.Empty;
            _currency = (Environment.GetEnvironmentVariable("STRIPE_CURRENCY")
                ?? config["Stripe:Currency"] ?? "brl").ToLowerInvariant();
            _appPublicUrl = (Environment.GetEnvironmentVariable("APP_PUBLIC_URL")
                ?? config["App:PublicUrl"] ?? "http://localhost:5050").TrimEnd('/');

            if (!string.IsNullOrEmpty(_secretKey))
                StripeConfiguration.ApiKey = _secretKey;
        }

        private void GarantirConfigurado()
        {
            if (string.IsNullOrEmpty(_secretKey))
                throw new InvalidOperationException(
                    "Stripe não configurado. Defina STRIPE_SECRET_KEY (sk_live_... ou sk_test_...).");
        }

        public async Task<CobrancaResult> CriarCobrancaAsync(int tenantId, int agendamentoId,
            decimal valor, FormaPagamento forma, string descricao, int expiracaoMinutos)
        {
            GarantirConfigurado();

            if (forma != FormaPagamento.CartaoCredito && forma != FormaPagamento.CartaoDebito)
            {
                throw new InvalidOperationException(
                    $"Stripe gateway aceita apenas cartão (forma recebida: {forma}). Use MercadoPago para PIX.");
            }

            // Stripe trabalha em centavos (long). Arredonda pra cima evita undercharge.
            var valorCentavos = (long)Math.Round(valor * 100m, MidpointRounding.AwayFromZero);
            var expiracao = DateTime.UtcNow.AddMinutes(Math.Max(expiracaoMinutos, 30));
            // Stripe exige expiração entre 30min e 24h.
            if (expiracao > DateTime.UtcNow.AddHours(24)) expiracao = DateTime.UtcNow.AddHours(24);

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = _currency,
                            UnitAmount = valorCentavos,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = string.IsNullOrWhiteSpace(descricao) ? "Agendamento" : descricao
                            }
                        }
                    }
                },
                ClientReferenceId = $"{tenantId}:{agendamentoId}",
                Metadata = new Dictionary<string, string>
                {
                    ["tenantId"] = tenantId.ToString(),
                    ["agendamentoId"] = agendamentoId.ToString()
                },
                SuccessUrl = $"{_appPublicUrl}/api/v1/webhooks/pagamento/Stripe/return?session_id={{CHECKOUT_SESSION_ID}}&status=ok",
                CancelUrl = $"{_appPublicUrl}/api/v1/webhooks/pagamento/Stripe/return?session_id={{CHECKOUT_SESSION_ID}}&status=cancelado",
                ExpiresAt = expiracao
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return new CobrancaResult
            {
                GatewayId = session.Id,
                QrCode = null, // checkout hospedado não usa QR
                LinkPagamento = session.Url,
                Expiracao = expiracao,
                PayloadBruto = System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionId = session.Id,
                    paymentIntent = session.PaymentIntentId
                })
            };
        }

        public Task<WebhookEvent> ProcessarWebhookAsync(string payload, string assinatura)
        {
            GarantirConfigurado();
            if (string.IsNullOrEmpty(_webhookSecret))
                throw new InvalidOperationException("STRIPE_WEBHOOK_SECRET não configurado.");

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(payload, assinatura, _webhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Assinatura Stripe inválida.");
                throw new UnauthorizedAccessException("Assinatura Stripe inválida.");
            }

            var status = MapearStatus(stripeEvent);
            var gatewayId = ExtrairGatewayId(stripeEvent);

            return Task.FromResult(new WebhookEvent
            {
                EventoId = stripeEvent.Id,
                GatewayId = gatewayId,
                Tipo = stripeEvent.Type,
                Status = status,
                PayloadBruto = payload
            });
        }

        private static StatusPagamento MapearStatus(Event ev)
        {
            return ev.Type switch
            {
                "checkout.session.completed" => StatusPagamento.Aprovado,
                "checkout.session.expired" => StatusPagamento.Expirado,
                "payment_intent.succeeded" => StatusPagamento.Aprovado,
                "payment_intent.payment_failed" => StatusPagamento.Recusado,
                "charge.refunded" => StatusPagamento.Estornado,
                _ => StatusPagamento.Pendente
            };
        }

        private static string ExtrairGatewayId(Event ev)
        {
            // checkout.session.* → session.Id; payment_intent.* → intent.Id; charge.* → charge.Id
            if (ev.Data?.Object is Session session) return session.Id;
            if (ev.Data?.Object is PaymentIntent intent) return intent.Id;
            if (ev.Data?.Object is Charge charge) return charge.PaymentIntentId ?? charge.Id;
            return ev.Id;
        }
    }
}

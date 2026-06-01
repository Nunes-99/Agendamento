using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgendamentoPro.Infrastructure.Services.Pagamento
{
    /// <summary>
    /// Integração com Mercado Pago Subscriptions (Preapproval API).
    /// Usado APENAS para mensalidade SaaS do tenant — não confundir com o MercadoPagoGateway
    /// (transacional do cliente final que paga pelo serviço agendado).
    ///
    /// Mesmas envs do gateway transacional:
    ///   MERCADOPAGO_ACCESS_TOKEN  → access token de produção
    ///   MERCADOPAGO_WEBHOOK_SECRET → secret do webhook
    ///   APP_PUBLIC_URL             → URL pública (para back_url e notification_url)
    /// </summary>
    public class MercadoPagoAssinaturaService : IGatewayAssinatura
    {
        private const string BaseUrl = "https://api.mercadopago.com";

        private readonly HttpClient _http;
        private readonly ILogger<MercadoPagoAssinaturaService> _logger;
        private readonly string _accessToken;
        private readonly string _webhookSecret;
        private readonly string _appPublicUrl;
        private readonly bool _isProduction;

        public string Nome => "MercadoPago";

        public MercadoPagoAssinaturaService(HttpClient http, IConfiguration config,
            ILogger<MercadoPagoAssinaturaService> logger)
        {
            _http = http;
            _logger = logger;
            _accessToken = Environment.GetEnvironmentVariable("MERCADOPAGO_ACCESS_TOKEN")
                ?? config["MercadoPago:AccessToken"] ?? string.Empty;
            _webhookSecret = Environment.GetEnvironmentVariable("MERCADOPAGO_WEBHOOK_SECRET")
                ?? config["MercadoPago:WebhookSecret"] ?? string.Empty;
            _appPublicUrl = (Environment.GetEnvironmentVariable("APP_PUBLIC_URL")
                ?? config["App:PublicUrl"] ?? "http://localhost:5050").TrimEnd('/');
            _isProduction = string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Production", StringComparison.OrdinalIgnoreCase);

            _http.BaseAddress = new Uri(BaseUrl);
            if (!string.IsNullOrEmpty(_accessToken))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        private void GarantirConfigurado()
        {
            if (string.IsNullOrEmpty(_accessToken))
                throw new InvalidOperationException(
                    "Mercado Pago não configurado. Defina MERCADOPAGO_ACCESS_TOKEN.");
        }

        public async Task<CriarAssinaturaGatewayResult> CriarPreapprovalAsync(
            int tenantId, int assinaturaId, decimal valor, string descricao,
            string payerEmail, string backUrl)
        {
            GarantirConfigurado();

            var payload = new
            {
                reason = descricao,
                external_reference = $"tenant:{tenantId}:assinatura:{assinaturaId}",
                payer_email = payerEmail,
                back_url = backUrl,
                auto_recurring = new
                {
                    frequency = 1,
                    frequency_type = "months",
                    transaction_amount = (double)valor,
                    currency_id = "BRL"
                },
                notification_url = $"{_appPublicUrl}/api/v1/webhooks/assinatura/MercadoPago",
                status = "pending"
            };

            var resp = await _http.PostAsJsonAsync("/preapproval", payload);
            await EnsureSuccess(resp, "Falha ao criar preapproval no Mercado Pago");

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            DateTime? proxVenc = null;
            if (root.TryGetProperty("next_payment_date", out var npd)
                && DateTime.TryParse(npd.GetString(), out var dt))
                proxVenc = dt.ToUniversalTime();

            return new CriarAssinaturaGatewayResult
            {
                PreapprovalId = root.GetProperty("id").GetString(),
                InitPointUrl = root.TryGetProperty("init_point", out var ip) ? ip.GetString() : null,
                ProximoVencimento = proxVenc,
                PayloadBruto = root.GetRawText()
            };
        }

        public async Task<bool> CancelarAsync(string preapprovalId)
        {
            GarantirConfigurado();
            if (string.IsNullOrWhiteSpace(preapprovalId)) return false;

            var resp = await _http.PutAsJsonAsync($"/preapproval/{preapprovalId}",
                new { status = "cancelled" });

            if (resp.IsSuccessStatusCode) return true;

            var body = await resp.Content.ReadAsStringAsync();
            _logger.LogWarning("MP cancelar preapproval {Id} falhou: {Status} {Body}",
                preapprovalId, resp.StatusCode, body);
            return false;
        }

        public async Task<bool> AtualizarValorAsync(string preapprovalId, decimal novoValor)
        {
            GarantirConfigurado();
            if (string.IsNullOrWhiteSpace(preapprovalId)) return false;

            var resp = await _http.PutAsJsonAsync($"/preapproval/{preapprovalId}", new
            {
                auto_recurring = new
                {
                    frequency = 1,
                    frequency_type = "months",
                    transaction_amount = (double)novoValor,
                    currency_id = "BRL"
                }
            });

            if (resp.IsSuccessStatusCode) return true;

            var body = await resp.Content.ReadAsStringAsync();
            _logger.LogWarning("MP atualizar valor preapproval {Id} falhou: {Status} {Body}",
                preapprovalId, resp.StatusCode, body);
            return false;
        }

        public async Task<WebhookAssinaturaEvent> ProcessarWebhookAsync(string payload, string assinaturaHeader)
        {
            GarantirConfigurado();
            if (string.IsNullOrWhiteSpace(payload)) return null;

            // FAIL CLOSED em produção sem secret.
            if (string.IsNullOrEmpty(_webhookSecret))
            {
                if (_isProduction)
                {
                    _logger.LogError("Webhook MP Assinatura: MERCADOPAGO_WEBHOOK_SECRET ausente em Production — rejeitando.");
                    return null;
                }
                _logger.LogWarning("Webhook MP Assinatura: sem secret (dev mode). Em produção seria recusado.");
            }
            else if (!MercadoPagoSignatureVerifier.Verificar(payload, assinaturaHeader, _webhookSecret))
            {
                _logger.LogWarning("Webhook MP Assinatura: assinatura inválida ou expirada.");
                return null;
            }

            string dataId, tipo, eventoId;
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("id", out var idEl))
                    return null;
                dataId = idEl.GetRawText().Trim('"');
                tipo = root.TryGetProperty("type", out var t) ? t.GetString()
                    : (root.TryGetProperty("action", out var a) ? a.GetString() : null);
                eventoId = root.TryGetProperty("id", out var nid) ? nid.GetRawText().Trim('"') : null;
            }
            catch (JsonException)
            {
                return null;
            }

            return tipo switch
            {
                "subscription_preapproval" => await ConsultarPreapprovalAsync(dataId, eventoId),
                "subscription_authorized_payment" => await ConsultarPagamentoRecorrenteAsync(dataId, eventoId),
                _ => new WebhookAssinaturaEvent
                {
                    EventoId = eventoId ?? $"{tipo}-{dataId}",
                    Tipo = TipoEventoAssinatura.Outro,
                    PreapprovalId = dataId,
                    PayloadBruto = payload
                }
            };
        }

        private async Task<WebhookAssinaturaEvent> ConsultarPreapprovalAsync(string preapprovalId, string eventoId)
        {
            var resp = await _http.GetAsync($"/preapproval/{preapprovalId}");
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("MP webhook: preapproval {Id} não localizado ({Status}).",
                    preapprovalId, resp.StatusCode);
                return null;
            }
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;

            var tipo = (status ?? "").ToLowerInvariant() switch
            {
                "authorized" => TipoEventoAssinatura.PreapprovalAutorizado,
                "paused" => TipoEventoAssinatura.PreapprovalPausado,
                "cancelled" or "finished" => TipoEventoAssinatura.PreapprovalCancelado,
                _ => TipoEventoAssinatura.Outro
            };

            DateTime? proxVenc = null;
            if (root.TryGetProperty("next_payment_date", out var npd)
                && DateTime.TryParse(npd.GetString(), out var dt))
                proxVenc = dt.ToUniversalTime();

            return new WebhookAssinaturaEvent
            {
                EventoId = eventoId ?? $"preapproval-{preapprovalId}-{status}",
                Tipo = tipo,
                PreapprovalId = preapprovalId,
                ProximoVencimento = proxVenc,
                PayloadBruto = root.GetRawText()
            };
        }

        private async Task<WebhookAssinaturaEvent> ConsultarPagamentoRecorrenteAsync(string authorizedPaymentId, string eventoId)
        {
            var resp = await _http.GetAsync($"/authorized_payments/{authorizedPaymentId}");
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("MP webhook: authorized_payment {Id} não localizado ({Status}).",
                    authorizedPaymentId, resp.StatusCode);
                return null;
            }
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            var preapprovalId = root.TryGetProperty("preapproval_id", out var pid) ? pid.GetString() : null;
            var statusStr = root.TryGetProperty("status", out var st) ? st.GetString() : null;

            var tipo = (statusStr ?? "").ToLowerInvariant() switch
            {
                "approved" or "processed" => TipoEventoAssinatura.PagamentoAprovado,
                "rejected" or "cancelled" => TipoEventoAssinatura.PagamentoRecusado,
                "refunded" or "charged_back" => TipoEventoAssinatura.PagamentoEstornado,
                _ => TipoEventoAssinatura.Outro
            };

            decimal? valor = null;
            if (root.TryGetProperty("transaction_amount", out var tv) && tv.TryGetDecimal(out var v))
                valor = v;

            DateTime? ocorreuEm = null;
            if (root.TryGetProperty("payment_date", out var pd)
                && DateTime.TryParse(pd.GetString(), out var dt))
                ocorreuEm = dt.ToUniversalTime();

            return new WebhookAssinaturaEvent
            {
                EventoId = eventoId ?? $"payment-{authorizedPaymentId}-{statusStr}",
                Tipo = tipo,
                PreapprovalId = preapprovalId,
                PaymentId = authorizedPaymentId,
                Valor = valor,
                OcorreuEm = ocorreuEm,
                PayloadBruto = root.GetRawText()
            };
        }

        private static async Task EnsureSuccess(HttpResponseMessage resp, string contexto)
        {
            if (resp.IsSuccessStatusCode) return;
            var body = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{contexto}: {(int)resp.StatusCode} {resp.ReasonPhrase} — {body}");
        }
    }
}

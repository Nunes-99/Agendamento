using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AgendamentoPro.Infrastructure.Services.Pagamento
{
    /// <summary>
    /// Integração real com Mercado Pago.
    /// - PIX: usa /v1/payments diretamente (gera QR Code)
    /// - Cartão crédito/débito: usa Checkout Pro (/checkout/preferences) com redirect
    /// - Webhook: verifica assinatura HMAC e consulta /v1/payments/{id} para status atual
    ///
    /// Configuração obrigatória (env var ou appsettings):
    ///   MERCADOPAGO_ACCESS_TOKEN  → Access token de produção da conta MP
    ///   MERCADOPAGO_WEBHOOK_SECRET → Secret do webhook (Mercado Pago > Notificações)
    ///   APP_PUBLIC_URL             → URL pública da API para callbacks
    /// </summary>
    public class MercadoPagoGateway : IGatewayPagamento
    {
        private const string BaseUrl = "https://api.mercadopago.com";

        /// <summary>
        /// Serialização para o Mercado Pago.
        ///
        /// O encoder padrão do System.Text.Json escapa o "+" do fuso horário como
        /// a sequência unicode escapada equivalente — JSON
        /// perfeitamente válido, mas o parser de datas do MP lê o texto ANTES de
        /// desescapar e devolve "error_parsing_date". Como o PIX usava offset
        /// "-03:00" (sem "+"), só o cartão quebrava: nenhum cliente conseguia
        /// pagar com crédito ou débito, com um 400 que não dizia o motivo.
        /// </summary>
        private static readonly JsonSerializerOptions MpJson = new(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly HttpClient _http;
        private readonly ILogger<MercadoPagoGateway> _logger;
        private readonly string _accessToken;
        private readonly string _webhookSecret;
        private readonly string _appPublicUrl;
        private readonly bool _isProduction;

        public string Nome => "MercadoPago";

        /// <summary>
        /// Sem access token, este gateway se declara INDISPONÍVEL em vez de aceitar
        /// a forma e estourar na hora de cobrar.
        ///
        /// A diferença aparece na tela do cliente: dizendo "suporto" e falhando
        /// depois, ele recebia um 500 e "Erro interno do servidor" — sem pista do
        /// que houve, e sem nada no radar do dono da oficina. Declarando-se
        /// indisponível, a escolha do gateway falha antes, com mensagem que o
        /// cliente entende e um erro de domínio (400) em vez de falha de servidor.
        /// </summary>
        public bool Suporta(FormaPagamento forma) =>
            !string.IsNullOrEmpty(_accessToken)
            && forma is FormaPagamento.Pix or FormaPagamento.CartaoCredito
                or FormaPagamento.CartaoDebito or FormaPagamento.Boleto;

        public MercadoPagoGateway(HttpClient http, IConfiguration config, ILogger<MercadoPagoGateway> logger)
        {
            _http = http;
            _logger = logger;
            _accessToken = Environment.GetEnvironmentVariable("MERCADOPAGO_ACCESS_TOKEN")
                ?? config["MercadoPago:AccessToken"]
                ?? string.Empty;
            _webhookSecret = Environment.GetEnvironmentVariable("MERCADOPAGO_WEBHOOK_SECRET")
                ?? config["MercadoPago:WebhookSecret"]
                ?? string.Empty;
            _appPublicUrl = (Environment.GetEnvironmentVariable("APP_PUBLIC_URL")
                ?? config["App:PublicUrl"]
                ?? "http://localhost:5050").TrimEnd('/');
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
                    "Mercado Pago não configurado. Defina a variável MERCADOPAGO_ACCESS_TOKEN com seu access token (produção ou TEST-* para desenvolvimento).");
        }

        /// <summary>
        /// E-mail do pagador aceito pelo MP: o do cliente quando parece válido; senão
        /// um fallback com domínio real — TLDs inventados (.local) tomam 400.
        /// </summary>
        /// <summary>URL alcançável pelo Mercado Pago (não localhost/IP privado).</summary>
        private static bool UrlPublica(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;
            var host = uri.Host;
            return !uri.IsLoopback
                && !host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                && !host.StartsWith("192.168.") && !host.StartsWith("10.")
                && !host.EndsWith(".local", StringComparison.OrdinalIgnoreCase);
        }

        private static string PayerEmailValido(string payerEmail)
            => !string.IsNullOrWhiteSpace(payerEmail) && payerEmail.Contains('@') && payerEmail.Contains('.')
                ? payerEmail.Trim()
                : "cliente-sem-email@agendamentopro.com.br";

        public async Task<CobrancaResult> CriarCobrancaAsync(int tenantId, int agendamentoId,
            decimal valor, FormaPagamento forma, string descricao, int expiracaoMinutos,
            string payerEmail = null)
        {
            GarantirConfigurado();
            return forma switch
            {
                FormaPagamento.Pix => await CriarPixAsync(tenantId, agendamentoId, valor, descricao, expiracaoMinutos, payerEmail),
                FormaPagamento.CartaoCredito or FormaPagamento.CartaoDebito
                    => await CriarPreferenciaCheckoutAsync(tenantId, agendamentoId, valor, forma, descricao, expiracaoMinutos),
                _ => throw new InvalidOperationException($"Forma de pagamento '{forma}' não suportada pelo Mercado Pago.")
            };
        }

        private async Task<CobrancaResult> CriarPixAsync(int tenantId, int agendamentoId,
            decimal valor, string descricao, int expiracaoMinutos, string payerEmail)
        {
            var idempotencyKey = $"agp-{tenantId}-{agendamentoId}-{Guid.NewGuid():N}";
            var dataExpiracao = DateTimeOffset.UtcNow.AddMinutes(expiracaoMinutos);

            var payload = new Dictionary<string, object>
            {
                ["transaction_amount"] = (double)valor,
                ["description"] = descricao,
                ["payment_method_id"] = "pix",
                ["date_of_expiration"] = FormatarDataMP(dataExpiracao),
                ["external_reference"] = $"{tenantId}:{agendamentoId}",
                ["payer"] = new { email = PayerEmailValido(payerEmail) }
            };
            // O MP recusa notification_url que não seja pública ("must be url valid"
            // para localhost) — e sem ela o PIX nem era criado em dev. Em dev sem
            // túnel, o webhook simplesmente não é chamado (use ngrok para testá-lo).
            if (UrlPublica(_appPublicUrl))
                payload["notification_url"] = $"{_appPublicUrl}/api/v1/webhooks/pagamento/MercadoPago";

            using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/payments")
            {
                Content = JsonContent.Create(payload, options: MpJson)
            };
            req.Headers.Add("X-Idempotency-Key", idempotencyKey);

            var resp = await _http.SendAsync(req);
            await EnsureSuccess(resp, "Falha ao criar PIX no Mercado Pago");

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var id = root.GetProperty("id").GetRawText();
            var poi = root.GetProperty("point_of_interaction").GetProperty("transaction_data");

            return new CobrancaResult
            {
                GatewayId = id,
                QrCode = poi.GetProperty("qr_code").GetString(),
                LinkPagamento = poi.TryGetProperty("ticket_url", out var tu) ? tu.GetString() : null,
                Expiracao = dataExpiracao.UtcDateTime,
                PayloadBruto = root.GetRawText()
            };
        }

        private async Task<CobrancaResult> CriarPreferenciaCheckoutAsync(int tenantId, int agendamentoId,
            decimal valor, FormaPagamento forma, string descricao, int expiracaoMinutos)
        {
            var agora = DateTimeOffset.UtcNow;
            var dataExpiracao = agora.AddMinutes(expiracaoMinutos);
            var paymentTypes = forma == FormaPagamento.CartaoDebito
                ? new[] { new { id = "credit_card" }, new { id = "ticket" } } // MP não tem "debit_card" exposto em prefs simples
                : new[] { new { id = "ticket" }, new { id = "atm" } };

            var payload = new Dictionary<string, object>
            {
                ["items"] = new[]
                {
                    new {
                        title = descricao,
                        quantity = 1,
                        currency_id = "BRL",
                        unit_price = (double)valor
                    }
                },
                ["external_reference"] = $"{tenantId}:{agendamentoId}",
                ["expires"] = true,
                // O MP exige a janela COMPLETA quando expires=true: só o "to" faz a
                // preferência ser recusada com error_parsing_date, e o cartão parava
                // de funcionar inteiro. DateTimeOffset + InvariantCulture porque o
                // formato ".fff" vira ",fff" em cultura pt-BR e o "zzz" de um
                // DateTime UTC carimba o offset local (hora UTC rotulada como -03:00).
                ["expiration_date_from"] = FormatarDataMP(agora),
                ["expiration_date_to"] = FormatarDataMP(dataExpiracao),
                ["payment_methods"] = new
                {
                    excluded_payment_types = paymentTypes,
                    installments = forma == FormaPagamento.CartaoDebito ? 1 : 12
                }
            };
            if (UrlPublica(_appPublicUrl))
                payload["notification_url"] = $"{_appPublicUrl}/api/v1/webhooks/pagamento/MercadoPago";

            var resp = await _http.PostAsJsonAsync("/checkout/preferences", payload, MpJson);
            await EnsureSuccess(resp, "Falha ao criar preferência no Mercado Pago",
                JsonSerializer.Serialize(payload, MpJson));

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            return new CobrancaResult
            {
                GatewayId = root.GetProperty("id").GetString(),
                LinkPagamento = root.GetProperty("init_point").GetString(),
                QrCode = null,
                Expiracao = dataExpiracao.UtcDateTime,
                PayloadBruto = root.GetRawText()
            };
        }

        public async Task<WebhookEvent> ProcessarWebhookAsync(string payload, string assinatura)
        {
            GarantirConfigurado();
            if (string.IsNullOrWhiteSpace(payload)) return null;

            // Verificação de assinatura (Mercado Pago x-signature: ts=...,v1=...).
            // Inclui validação do timestamp (replay protection - aceita até 5 min de diferença).
            //
            // FAIL CLOSED em produção: sem MERCADOPAGO_WEBHOOK_SECRET configurado,
            // qualquer requisição externa marcaria pagamentos como aprovados sem
            // validação — vulnerabilidade crítica. Em dev permite (no-op silencioso)
            // pra facilitar testes manuais.
            if (string.IsNullOrEmpty(_webhookSecret))
            {
                if (_isProduction)
                {
                    _logger.LogError(
                        "Webhook MercadoPago: MERCADOPAGO_WEBHOOK_SECRET não configurado em Production — rejeitando.");
                    return null;
                }
                _logger.LogWarning(
                    "Webhook MercadoPago: sem secret configurado (modo dev). Em produção isso seria recusado.");
            }
            else
            {
                if (string.IsNullOrEmpty(assinatura))
                {
                    _logger.LogWarning("Webhook MercadoPago: assinatura ausente em ambiente com webhookSecret configurado.");
                    return null;
                }
                if (!VerificarAssinaturaMP(payload, assinatura))
                {
                    _logger.LogWarning("Webhook MercadoPago: assinatura inválida ou expirada.");
                    return null;
                }
            }

            string paymentId;
            string eventoId = null;
            string tipo = null;
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("id", out var idEl))
                    return null;
                paymentId = idEl.GetRawText().Trim('"');

                // Top-level "id" é o ID único da notificação - chave de idempotência.
                if (root.TryGetProperty("id", out var notifIdEl))
                    eventoId = notifIdEl.GetRawText().Trim('"');
                if (root.TryGetProperty("action", out var actionEl))
                    tipo = actionEl.GetString();
                else if (root.TryGetProperty("type", out var typeEl))
                    tipo = typeEl.GetString();
            }
            catch (JsonException)
            {
                return null;
            }

            // Consulta status atual via API (webhook não traz status detalhado)
            var resp = await _http.GetAsync($"/v1/payments/{paymentId}");
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Webhook MercadoPago: payment {Id} não localizado ({Status}).",
                    paymentId, resp.StatusCode);
                return null;
            }

            using var pagDoc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var pagRoot = pagDoc.RootElement;
            var statusStr = pagRoot.TryGetProperty("status", out var st) ? st.GetString() : null;

            // Fallback: se não veio top-level id, usa "{paymentId}-{status}" como chave de evento.
            // Não é ideal mas evita reprocessar o mesmo status repetidas vezes.
            if (string.IsNullOrEmpty(eventoId))
                eventoId = $"{paymentId}-{statusStr ?? "unknown"}";

            return new WebhookEvent
            {
                GatewayId = paymentId,
                EventoId = eventoId,
                Tipo = tipo,
                Status = MapearStatus(statusStr),
                PayloadBruto = pagRoot.GetRawText()
            };
        }

        private static StatusPagamento MapearStatus(string mpStatus) => (mpStatus ?? "").ToLowerInvariant() switch
        {
            "approved" => StatusPagamento.Aprovado,
            "rejected" or "cancelled" => StatusPagamento.Recusado,
            "refunded" or "charged_back" => StatusPagamento.Estornado,
            "expired" => StatusPagamento.Expirado,
            _ => StatusPagamento.Pendente
        };

        // Janela de tolerância para o timestamp do webhook (replay protection).
        private static readonly TimeSpan WebhookMaxIdade = TimeSpan.FromMinutes(5);

        private bool VerificarAssinaturaMP(string payload, string assinatura)
        {
            // Header formato: "ts=<timestamp>,v1=<hmac>"
            var partes = assinatura.Split(',', StringSplitOptions.RemoveEmptyEntries);
            string ts = null, v1 = null;
            foreach (var p in partes)
            {
                var kv = p.Trim().Split('=', 2);
                if (kv.Length != 2) continue;
                if (kv[0] == "ts") ts = kv[1];
                else if (kv[0] == "v1") v1 = kv[1];
            }
            if (ts == null || v1 == null) return false;

            // Replay protection: rejeita timestamps muito antigos (ou no futuro distante).
            if (long.TryParse(ts, out var tsUnixMs))
            {
                var quandoUtc = DateTimeOffset.FromUnixTimeMilliseconds(tsUnixMs).UtcDateTime;
                var diff = (DateTime.UtcNow - quandoUtc).Duration();
                if (diff > WebhookMaxIdade)
                {
                    _logger.LogWarning("Webhook MercadoPago: ts fora da janela ({Diff}s).", diff.TotalSeconds);
                    return false;
                }
            }
            else
            {
                _logger.LogWarning("Webhook MercadoPago: ts não numérico no header de assinatura.");
                return false;
            }

            // MP assina: id (data.id) + ; + ts + ; → HMAC-SHA256 com webhookSecret
            string id = null;
            try
            {
                using var d = JsonDocument.Parse(payload);
                if (d.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idEl))
                    id = idEl.GetRawText().Trim('"');
            }
            catch { return false; }
            if (id == null) return false;

            var payloadAssinatura = $"id:{id};ts:{ts};";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadAssinatura));
            var hex = Convert.ToHexString(hash).ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hex), Encoding.UTF8.GetBytes(v1.ToLowerInvariant()));
        }

        /// <summary>
        /// Data no formato que o Mercado Pago aceita, sempre em cultura invariante:
        /// "2026-08-30T10:00:00.000-03:00".
        /// </summary>
        private static string FormatarDataMP(DateTimeOffset valor)
            => valor.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);

        /// <summary>
        /// Erro do gateway com o corpo enviado junto: sem ele, um "bad_request" do MP
        /// só dizia qual campo reclamou, nunca o que foi mandado nele.
        /// </summary>
        private static async Task EnsureSuccess(HttpResponseMessage resp, string contexto,
            string corpoEnviado = null)
        {
            if (resp.IsSuccessStatusCode) return;
            var body = await resp.Content.ReadAsStringAsync();
            var enviado = corpoEnviado == null ? "" : $" — enviado: {corpoEnviado}";
            throw new HttpRequestException(
                $"{contexto}: {(int)resp.StatusCode} {resp.ReasonPhrase} — {body}{enviado}");
        }
    }
}

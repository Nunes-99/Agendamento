using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AgendamentoPro.Infrastructure.Services.WhatsApp
{
    /// <summary>
    /// Integração real com WhatsApp Business Cloud API (Meta).
    ///
    /// Configuração obrigatória (env var ou appsettings):
    ///   WHATSAPP_ACCESS_TOKEN     → System User Access Token (graph.facebook.com)
    ///   WHATSAPP_PHONE_NUMBER_ID  → ID do número que envia as mensagens
    ///   WHATSAPP_API_VERSION      → opcional, default "v19.0"
    ///
    /// Para mensagens proativas (lembretes, confirmações), use templates pré-aprovados:
    ///   await EnviarTemplateAsync(numero, "agendamento_confirmado", parametros);
    /// </summary>
    public class WhatsAppCloudNotificador : INotificadorWhatsApp
    {
        private readonly HttpClient _http;
        private readonly ILogger<WhatsAppCloudNotificador> _logger;
        private readonly string _accessToken;
        private readonly string _phoneNumberId;
        private readonly string _apiVersion;
        private readonly bool _ativo;
        public bool Ativo => _ativo;

        public WhatsAppCloudNotificador(HttpClient http, IConfiguration config,
            ILogger<WhatsAppCloudNotificador> logger)
        {
            _http = http;
            _logger = logger;
            _accessToken = Environment.GetEnvironmentVariable("WHATSAPP_ACCESS_TOKEN")
                ?? config["WhatsApp:AccessToken"] ?? string.Empty;
            _phoneNumberId = Environment.GetEnvironmentVariable("WHATSAPP_PHONE_NUMBER_ID")
                ?? config["WhatsApp:PhoneNumberId"] ?? string.Empty;
            _apiVersion = Environment.GetEnvironmentVariable("WHATSAPP_API_VERSION")
                ?? config["WhatsApp:ApiVersion"] ?? "v19.0";

            _ativo = !string.IsNullOrWhiteSpace(_accessToken) && !string.IsNullOrWhiteSpace(_phoneNumberId);
            if (_ativo)
            {
                _http.BaseAddress = new Uri("https://graph.facebook.com/");
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
            else
            {
                _logger.LogWarning("WhatsApp Cloud API não configurado. Mensagens só gerarão links wa.me.");
            }
        }

        public async Task EnviarAsync(string numero, string mensagem)
        {
            var num = SanitizarNumero(numero);
            if (string.IsNullOrEmpty(num)) return;

            if (!_ativo)
            {
                _logger.LogInformation("WhatsApp inativo. Link gerado: {Link}", GerarLinkWhatsApp(numero, mensagem));
                return;
            }

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = num,
                type = "text",
                text = new { preview_url = false, body = mensagem }
            };

            var resp = await _http.PostAsJsonAsync($"{_apiVersion}/{_phoneNumberId}/messages", payload);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Falha ao enviar WhatsApp para {Numero}: {Status} {Body}",
                    num, resp.StatusCode, body);
                throw new HttpRequestException($"WhatsApp Cloud API erro: {resp.StatusCode} - {body}");
            }
        }

        /// <summary>
        /// Envia mensagem usando template pré-aprovado pela Meta.
        /// Necessário para mensagens proativas fora da janela de 24h.
        /// </summary>
        public async Task EnviarTemplateAsync(string numero, string templateName,
            string idiomaCodigo = "pt_BR", params string[] parametros)
        {
            var num = SanitizarNumero(numero);
            if (string.IsNullOrEmpty(num) || !_ativo) return;

            var components = parametros.Length == 0 ? null : new[]
            {
                new
                {
                    type = "body",
                    parameters = parametros.Select(p => new { type = "text", text = p }).ToArray()
                }
            };

            var payload = new
            {
                messaging_product = "whatsapp",
                to = num,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = idiomaCodigo },
                    components
                }
            };

            var resp = await _http.PostAsJsonAsync($"{_apiVersion}/{_phoneNumberId}/messages", payload);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogError("Falha template WhatsApp {Template} para {Numero}: {Status} {Body}",
                    templateName, num, resp.StatusCode, body);
                throw new HttpRequestException($"WhatsApp Cloud API erro: {resp.StatusCode} - {body}");
            }
        }

        public string GerarLinkWhatsApp(string numero, string mensagem)
        {
            var num = SanitizarNumero(numero);
            var texto = Uri.EscapeDataString(mensagem ?? string.Empty);
            return $"https://wa.me/{num}?text={texto}";
        }

        private static string SanitizarNumero(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero)) return string.Empty;
            var apenasDigitos = new string(numero.Where(char.IsDigit).ToArray());
            // Se for número BR sem DDI (10 ou 11 dígitos), prefixa 55
            if (apenasDigitos.Length == 10 || apenasDigitos.Length == 11)
                apenasDigitos = "55" + apenasDigitos;
            return apenasDigitos;
        }
    }
}

using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgendamentoPro.Infrastructure.Services.Auth
{
    /// <summary>
    /// Validador Google reCAPTCHA v3 via API HTTP. Configuração via env:
    ///   RECAPTCHA_SECRET_KEY (do painel Google reCAPTCHA Admin Console)
    ///
    /// Free tier: 1M requests/mês. Sem cobrança.
    /// Site: https://www.google.com/recaptcha/admin/create
    /// </summary>
    public class RecaptchaValidator : IRecaptchaValidator
    {
        private readonly HttpClient _http;
        private readonly string _secret;
        private readonly ILogger<RecaptchaValidator> _logger;

        public bool Ativo => !string.IsNullOrEmpty(_secret);

        public RecaptchaValidator(HttpClient http, IConfiguration config, ILogger<RecaptchaValidator> logger)
        {
            _http = http;
            _logger = logger;
            _secret = Environment.GetEnvironmentVariable("RECAPTCHA_SECRET_KEY")
                ?? config["Recaptcha:SecretKey"] ?? "";
        }

        public async Task<bool> ValidarAsync(string token, string acao, double scoreMinimo = 0.5)
        {
            if (!Ativo) return true; // dev/no-op
            if (string.IsNullOrEmpty(token)) return false;

            try
            {
                var resp = await _http.PostAsync(
                    $"https://www.google.com/recaptcha/api/siteverify?secret={_secret}&response={token}",
                    null);
                if (!resp.IsSuccessStatusCode) return false;

                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                var root = doc.RootElement;
                var success = root.GetProperty("success").GetBoolean();
                if (!success) return false;

                if (root.TryGetProperty("score", out var scoreEl))
                {
                    var score = scoreEl.GetDouble();
                    if (score < scoreMinimo)
                    {
                        _logger.LogWarning("reCAPTCHA score baixo: {Score} (acao={Acao})", score, acao);
                        return false;
                    }
                }
                if (!string.IsNullOrEmpty(acao) && root.TryGetProperty("action", out var actEl))
                {
                    if (!actEl.GetString().Equals(acao, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao validar reCAPTCHA");
                return false;
            }
        }
    }
}

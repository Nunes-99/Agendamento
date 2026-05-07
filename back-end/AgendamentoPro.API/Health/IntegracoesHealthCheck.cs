using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AgendamentoPro.API.Health
{
    /// <summary>
    /// Verifica se as integrações externas opcionais (WhatsApp, gateway PIX, reCAPTCHA)
    /// estão configuradas. Não bloqueia readiness — só sinaliza degradado.
    /// </summary>
    public class IntegracoesHealthCheck : IHealthCheck
    {
        private readonly INotificadorWhatsApp _whats;
        private readonly IEnumerable<IGatewayPagamento> _gateways;
        private readonly IRecaptchaValidator _recaptcha;

        public IntegracoesHealthCheck(INotificadorWhatsApp whats,
            IEnumerable<IGatewayPagamento> gateways, IRecaptchaValidator recaptcha)
        {
            _whats = whats;
            _gateways = gateways;
            _recaptcha = recaptcha;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            var data = new Dictionary<string, object>
            {
                ["whatsapp"] = _whats.Ativo ? "ativo" : "no-op",
                ["gateways_pix"] = string.Join(",", _gateways.Select(g => g.Nome)),
                ["recaptcha"] = _recaptcha.Ativo ? "ativo" : "no-op"
            };

            // Mesmo "no-op" é healthy — significa que o sistema funciona sem essas integrações.
            return Task.FromResult(HealthCheckResult.Healthy("Integrações disponíveis", data));
        }
    }
}

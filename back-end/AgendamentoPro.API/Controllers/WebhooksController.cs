using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Application.Interfaces.Pagamentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/webhooks")]
    [AllowAnonymous]
    [EnableRateLimiting("webhook")]
    [Produces("application/json")]
    public class WebhooksController : ControllerBase
    {
        [HttpPost("pagamento/{gateway}")]
        public async Task<IActionResult> Pagamento(
            [FromServices] IProcessarWebhookPagamentoUseCase useCase,
            string gateway,
            [FromHeader(Name = "X-Signature")] string xSignature = null,
            [FromHeader(Name = "Stripe-Signature")] string stripeSignature = null)
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();
            // Cada gateway envia em seu próprio header — usa o primeiro disponível.
            var assinatura = !string.IsNullOrEmpty(stripeSignature) ? stripeSignature : xSignature;
            await useCase.ExecuteAsync(gateway, payload, assinatura);
            return Ok(new { received = true });
        }

        /// <summary>
        /// Webhook de cobrança recorrente SaaS (mensalidade do tenant).
        /// Eventos: subscription_preapproval + subscription_authorized_payment.
        /// </summary>
        [HttpPost("assinatura/{gateway}")]
        public async Task<IActionResult> Assinatura(
            [FromServices] IProcessarWebhookAssinaturaUseCase useCase,
            string gateway,
            [FromHeader(Name = "X-Signature")] string xSignature = null)
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();
            await useCase.ExecuteAsync(gateway, payload, xSignature);
            return Ok(new { received = true });
        }
    }
}

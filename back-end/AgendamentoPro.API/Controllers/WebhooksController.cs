using AgendamentoPro.Application.Interfaces.Pagamentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    [AllowAnonymous]
    [EnableRateLimiting("webhook")]
    [Produces("application/json")]
    public class WebhooksController : ControllerBase
    {
        [HttpPost("pagamento/{gateway}")]
        public async Task<IActionResult> Pagamento(
            [FromServices] IProcessarWebhookPagamentoUseCase useCase,
            string gateway,
            [FromHeader(Name = "X-Signature")] string assinatura = null)
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();
            await useCase.ExecuteAsync(gateway, payload, assinatura);
            return Ok(new { received = true });
        }
    }
}

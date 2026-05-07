using AgendamentoPro.Application.InputModels.Auth;
using AgendamentoPro.Application.Interfaces.Auth;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Produces("application/json")]
    [EnableRateLimiting("otp")]
    public class OtpController : BaseTenantController
    {
        /// <summary>Solicita um código OTP via WhatsApp para o telefone informado.</summary>
        [HttpPost("api/v1/t/{slug}/otp/solicitar")]
        [AllowAnonymous]
        public async Task<IActionResult> Solicitar(
            [FromServices] IOtpUseCase useCase,
            [FromServices] ITenantContext tenant,
            string slug, [FromBody] SolicitarOtpInputModel input)
        {
            var tid = RequireTenantId(tenant);
            var result = await useCase.SolicitarAsync(tid, tenant.Slug, input);
            return Ok(result);
        }

        /// <summary>Valida o código OTP e retorna um token JWT de cliente final (validade 7 dias).</summary>
        [HttpPost("api/v1/t/{slug}/otp/validar")]
        [AllowAnonymous]
        public async Task<IActionResult> Validar(
            [FromServices] IOtpUseCase useCase,
            [FromServices] ITenantContext tenant,
            string slug, [FromBody] ValidarOtpInputModel input)
        {
            var tid = RequireTenantId(tenant);
            var result = await useCase.ValidarAsync(tid, tenant.Slug, input);
            if (!result.Valido) return BadRequest(result);
            return Ok(result);
        }
    }
}

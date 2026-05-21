using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/web-push")]
    [Authorize(Policy = "Atendente")]
    [Produces("application/json")]
    public class WebPushController : BaseTenantController
    {
        /// <summary>
        /// Helper de DEV para gerar o par VAPID. Em produção o par é configurado uma vez
        /// via env e nunca rotacionado (browsers cacheiam a chave pública na subscription).
        /// </summary>
        [HttpPost("generate-keys")]
        [AllowAnonymous]
        public IActionResult GenerateKeys([FromServices] IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
                return NotFound();
            var keys = WebPush.VapidHelper.GenerateVapidKeys();
            return Ok(new
            {
                publicKey = keys.PublicKey,
                privateKey = keys.PrivateKey,
                instrucao = "Copie estes valores para VAPID_PUBLIC_KEY e VAPID_PRIVATE_KEY no .env e reinicie. Nunca rotacione em produção."
            });
        }

        public class SubscribeInput
        {
            public string Endpoint { get; set; }
            public string P256dh { get; set; }
            public string Auth { get; set; }
            public string UserAgent { get; set; }
        }

        /// <summary>Chave pública VAPID — o frontend usa pra criar a subscription.</summary>
        [HttpGet("vapid-key")]
        [AllowAnonymous]
        public IActionResult VapidKey([FromServices] IWebPushSender sender)
            => Ok(new { ativo = sender.Ativo, chavePublica = sender.ChavePublica });

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe(
            [FromServices] IWebPushSubscriptionRepository repo,
            [FromServices] ITenantContext tenant,
            [FromBody] SubscribeInput input)
        {
            var tid = RequireTenantId(tenant);
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Usuário inválido." });

            // Idempotente: se já existe pelo endpoint, atualiza ao invés de duplicar.
            var existente = await repo.GetByEndpointAsync(input.Endpoint);
            if (existente != null) return Ok(new { id = existente.PushId, atualizada = true });

            var sub = new WebPushSubscription(tid, userId, input.Endpoint, input.P256dh, input.Auth, input.UserAgent);
            await repo.CreateAsync(sub);
            return Ok(new { id = sub.PushId, atualizada = false });
        }

        [HttpDelete("subscribe")]
        public async Task<IActionResult> Unsubscribe(
            [FromServices] IWebPushSubscriptionRepository repo,
            [FromQuery] string endpoint)
        {
            await repo.DeleteByEndpointAsync(endpoint);
            return NoContent();
        }
    }
}

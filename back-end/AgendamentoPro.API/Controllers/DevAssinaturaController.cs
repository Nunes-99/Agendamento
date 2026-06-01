using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Endpoints de desenvolvimento para testar fluxo de assinatura sem precisar de webhook real do MP.
    /// Retorna 404 em ambiente Production — ver guard no início de cada action.
    /// </summary>
    [ApiController]
    [Route("api/v1/admin/assinatura/dev")]
    [Produces("application/json")]
    [Authorize(Policy = "AdminTenant")]
    public class DevAssinaturaController : BaseTenantController
    {
        private static IActionResult BloquearSeProducao(IHostEnvironment env)
            => env.IsProduction() ? new NotFoundResult() : null;

        /// <summary>
        /// Simula um pagamento aprovado: cria FaturaAssinatura paga + chama RegistrarPagamento
        /// na assinatura do tenant. Útil para sair do estado Atrasada/ReadOnly sem webhook do MP.
        /// </summary>
        [HttpPost("simular-pagamento")]
        public async Task<IActionResult> SimularPagamento(
            [FromServices] IHostEnvironment env,
            [FromServices] ISimularPagamentoAssinaturaUseCase useCase,
            [FromServices] ITenantContext ctx)
        {
            var bloqueio = BloquearSeProducao(env); if (bloqueio != null) return bloqueio;
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid));
        }

        /// <summary>
        /// Força um status específico na assinatura do tenant. Útil para inspecionar a UI em
        /// cada estado (banner amarelo/vermelho, bloqueio de escrita, etc.).
        /// Aceita: Ativa, Atrasada, ReadOnly, Cancelada, Expirada.
        /// </summary>
        [HttpPost("forcar-status")]
        public async Task<IActionResult> ForcarStatus(
            [FromServices] IHostEnvironment env,
            [FromServices] IForcarStatusAssinaturaUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] StatusAssinatura status)
        {
            var bloqueio = BloquearSeProducao(env); if (bloqueio != null) return bloqueio;
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid, status));
        }
    }

    /// <summary>
    /// Seed de demonstração para SuperAdmin — cria 5 tenants em estados distintos
    /// (Ativa, Atrasada, ReadOnly, Cancelada, Expirada) para inspeção rápida da UI.
    /// </summary>
    [ApiController]
    [Route("api/v1/superadmin/dev")]
    [Authorize(Policy = "SuperAdmin")]
    [Produces("application/json")]
    public class DevSuperAdminController : ControllerBase
    {
        [HttpPost("seed-assinaturas-demo")]
        public async Task<IActionResult> SeedAssinaturasDemo(
            [FromServices] IHostEnvironment env,
            [FromServices] ISeedAssinaturasDemoUseCase useCase)
        {
            if (env.IsProduction()) return NotFound();
            return Ok(await useCase.ExecuteAsync());
        }
    }
}

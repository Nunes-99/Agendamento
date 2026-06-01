using AgendamentoPro.Application.InputModels.Assinaturas;
using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Gerenciamento da assinatura SaaS do tenant logado (mensalidade para usar o sistema).
    /// Não confundir com /api/v1/admin/pagamentos (transacional do cliente final).
    /// </summary>
    [ApiController]
    [Route("api/v1/admin/assinatura")]
    [Produces("application/json")]
    [Authorize(Policy = "AdminTenant")]
    public class AssinaturasController : BaseTenantController
    {
        [HttpGet]
        public async Task<IActionResult> Minha(
            [FromServices] IMinhaAssinaturaUseCase useCase,
            [FromServices] ITenantContext ctx)
        {
            var tid = RequireTenantId(ctx);
            var ass = await useCase.ExecuteAsync(tid);
            return ass == null ? NotFound() : Ok(ass);
        }

        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromServices] ICriarAssinaturaUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromBody] CriarAssinaturaInputModel input)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid, input));
        }

        [HttpPut("plano")]
        public async Task<IActionResult> AlterarPlano(
            [FromServices] IAlterarPlanoUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromBody] AlterarPlanoInputModel input)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid, input));
        }

        [HttpDelete]
        public async Task<IActionResult> Cancelar(
            [FromServices] ICancelarAssinaturaUseCase useCase,
            [FromServices] ITenantContext ctx)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid));
        }
    }
}

using AgendamentoPro.Application.InputModels.Servicos;
using AgendamentoPro.Application.Interfaces.Servicos;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class CombosController : BaseTenantController
    {
        // ----- Endpoints públicos (catalog) -----

        [HttpGet("api/v1/t/{slug}/combos")]
        [AllowAnonymous]
        public async Task<IActionResult> ListarPublico(
            [FromServices] IComboUseCase useCase,
            [FromServices] ITenantContext ctx, string slug)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ListarAsync(tid, somenteAtivos: true));
        }

        [HttpPost("api/v1/t/{slug}/combos/{id:int}/agendar")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> AgendarPublico(
            [FromServices] IAgendarComboUseCase useCase,
            [FromServices] ITenantContext ctx, string slug, int id,
            [FromBody] AgendarComboInputModel input)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid, id, input));
        }

        // ----- Endpoints administrativos -----

        [HttpGet("api/v1/admin/combos")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Listar(
            [FromServices] IComboUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] bool somenteAtivos = false)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ListarAsync(tid, somenteAtivos));
        }

        [HttpGet("api/v1/admin/combos/{id:int}")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Obter(
            [FromServices] IComboUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            var tid = RequireTenantId(ctx);
            var v = await useCase.ObterAsync(tid, id);
            return v == null ? NotFound() : Ok(v);
        }

        [HttpPost("api/v1/admin/combos")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Criar(
            [FromServices] IComboUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromBody] ComboInputModel input)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.CriarAsync(tid, input));
        }

        [HttpPut("api/v1/admin/combos/{id:int}")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Atualizar(
            [FromServices] IComboUseCase useCase,
            [FromServices] ITenantContext ctx, int id,
            [FromBody] ComboInputModel input)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.AtualizarAsync(tid, id, input));
        }

        [HttpDelete("api/v1/admin/combos/{id:int}")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Remover(
            [FromServices] IComboUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            var tid = RequireTenantId(ctx);
            await useCase.RemoverAsync(tid, id);
            return NoContent();
        }
    }
}

using AgendamentoPro.Application.InputModels.Servicos;
using AgendamentoPro.Application.Interfaces.Servicos;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class ServicosController : BaseTenantController
    {
        // Endpoint público para o site do tenant
        [HttpGet("api/v1/t/{slug}/servicos")]
        [AllowAnonymous]
        public async Task<IActionResult> ListarPublico(
            [FromServices] IConsultarServicoUseCase useCase,
            [FromServices] ITenantContext ctx, string slug)
        {
            var tenantId = RequireTenantId(ctx);
            return Ok(await useCase.ListarAsync(tenantId, somenteAtivos: true));
        }

        [HttpGet("api/v1/admin/servicos")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> ListarAdmin(
            [FromServices] IConsultarServicoUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] bool somenteAtivos = false)
        {
            var tenantId = RequireTenantId(ctx);
            return Ok(await useCase.ListarAsync(tenantId, somenteAtivos));
        }

        [HttpGet("api/v1/admin/servicos/{id:int}")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> PorId(
            [FromServices] IConsultarServicoUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            var tenantId = RequireTenantId(ctx);
            var s = await useCase.PorIdAsync(tenantId, id);
            return s == null ? NotFound() : Ok(s);
        }

        [HttpPost("api/v1/admin/servicos")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Cadastrar(
            [FromServices] ICadastrarServicoUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromBody] ServicoInputModel input)
        {
            var tenantId = RequireTenantId(ctx);
            var s = await useCase.ExecuteAsync(tenantId, input);
            return CreatedAtAction(nameof(PorId), new { id = s.Id }, s);
        }

        [HttpPut("api/v1/admin/servicos/{id:int}")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Atualizar(
            [FromServices] IAtualizarServicoUseCase useCase,
            [FromServices] ITenantContext ctx, int id,
            [FromBody] ServicoInputModel input)
        {
            var tenantId = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tenantId, id, input));
        }

        [HttpDelete("api/v1/admin/servicos/{id:int}")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Excluir(
            [FromServices] IInativarServicoUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            var tenantId = RequireTenantId(ctx);
            await useCase.ExecuteAsync(tenantId, id);
            return NoContent();
        }
    }
}

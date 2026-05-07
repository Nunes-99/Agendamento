using AgendamentoPro.Application.InputModels.Recursos;
using AgendamentoPro.Application.Interfaces.Recursos;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/recursos")]
    [Produces("application/json")]
    [Authorize(Policy = "Atendente")]
    public class RecursosController : BaseTenantController
    {
        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromServices] IConsultarRecursoUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] bool somenteAtivos = false)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ListarAsync(tid, somenteAtivos));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> PorId(
            [FromServices] IConsultarRecursoUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            var tid = RequireTenantId(ctx);
            var r = await useCase.PorIdAsync(tid, id);
            return r == null ? NotFound() : Ok(r);
        }

        [HttpPost]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Cadastrar(
            [FromServices] ICadastrarRecursoUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromBody] RecursoInputModel input)
        {
            var tid = RequireTenantId(ctx);
            var r = await useCase.ExecuteAsync(tid, input);
            return CreatedAtAction(nameof(PorId), new { id = r.Id }, r);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Atualizar(
            [FromServices] IAtualizarRecursoUseCase useCase,
            [FromServices] ITenantContext ctx, int id,
            [FromBody] RecursoInputModel input)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid, id, input));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Excluir(
            [FromServices] IInativarRecursoUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            var tid = RequireTenantId(ctx);
            await useCase.ExecuteAsync(tid, id);
            return NoContent();
        }
    }
}

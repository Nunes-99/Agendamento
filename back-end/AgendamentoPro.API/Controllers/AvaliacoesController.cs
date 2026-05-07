using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Application.Interfaces.Agendamentos;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class AvaliacoesController : BaseTenantController
    {
        // ----- Endpoints públicos (cliente final) -----

        [HttpGet("api/v1/avaliacoes/{token:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> Buscar(
            [FromServices] IAvaliacaoUseCase useCase, Guid token)
        {
            var v = await useCase.BuscarPorTokenAsync(token);
            return v == null ? NotFound() : Ok(v);
        }

        [HttpPost("api/v1/avaliacoes/{token:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> Responder(
            [FromServices] IAvaliacaoUseCase useCase, Guid token,
            [FromBody] ResponderAvaliacaoInputModel input)
            => Ok(await useCase.ResponderAsync(token, input));

        [HttpGet("api/v1/t/{slug}/avaliacoes")]
        [AllowAnonymous]
        public async Task<IActionResult> ResumoPublico(
            [FromServices] IAvaliacaoUseCase useCase,
            [FromServices] ITenantContext ctx, string slug,
            [FromQuery] int top = 5)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ResumoAsync(tid, Math.Clamp(top, 1, 50)));
        }

        // ----- Endpoints administrativos -----

        [HttpPost("api/v1/admin/agendamentos/{agendamentoId:int}/avaliacao-link")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> ObterLinkAvaliacao(
            [FromServices] IAvaliacaoUseCase useCase,
            [FromServices] ITenantContext ctx, int agendamentoId)
        {
            var tid = RequireTenantId(ctx);
            var token = await useCase.AbrirAsync(tid, agendamentoId);
            return Ok(new { token, path = $"/avaliar/{token}" });
        }

        [HttpGet("api/v1/admin/avaliacoes")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Listar(
            [FromServices] IAvaliacaoUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] bool somenteRespondidas = false)
        {
            var tid = RequireTenantId(ctx);
            var (items, total) = await useCase.ListarAsync(tid, page, pageSize, somenteRespondidas);
            return Ok(new { items, total, page, pageSize });
        }

        [HttpPost("api/v1/admin/avaliacoes/{id:int}/visibilidade")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> AlterarVisibilidade(
            [FromServices] IAvaliacaoUseCase useCase,
            [FromServices] ITenantContext ctx, int id,
            [FromQuery] bool publica)
        {
            var tid = RequireTenantId(ctx);
            await useCase.AlterarVisibilidadeAsync(tid, id, publica);
            return NoContent();
        }
    }
}

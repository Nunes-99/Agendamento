using AgendamentoPro.Application.Interfaces.Relatorios;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/relatorios")]
    [Authorize(Policy = "AdminTenant")]
    [Produces("application/json")]
    public class RelatoriosController : BaseTenantController
    {
        [HttpGet("receita")]
        public async Task<IActionResult> Receita([FromServices] IRelatoriosUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] DateTime inicio, [FromQuery] DateTime fim)
            => Ok(await useCase.ReceitaPorDiaAsync(RequireTenantId(ctx), inicio, fim));

        [HttpGet("servicos-mais-vendidos")]
        public async Task<IActionResult> Top([FromServices] IRelatoriosUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] DateTime inicio, [FromQuery] DateTime fim)
            => Ok(await useCase.ServicosMaisVendidosAsync(RequireTenantId(ctx), inicio, fim));

        [HttpGet("ocupacao")]
        public async Task<IActionResult> Ocupacao([FromServices] IRelatoriosUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] DateTime inicio, [FromQuery] DateTime fim)
            => Ok(await useCase.TaxaOcupacaoAsync(RequireTenantId(ctx), inicio, fim));

        [HttpGet("cancelamentos")]
        public async Task<IActionResult> Cancelamentos([FromServices] IRelatoriosUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] DateTime inicio, [FromQuery] DateTime fim)
            => Ok(await useCase.CancelamentosAsync(RequireTenantId(ctx), inicio, fim));
    }
}

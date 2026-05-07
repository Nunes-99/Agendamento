using AgendamentoPro.Application.InputModels.Clientes;
using AgendamentoPro.Application.Interfaces.Clientes;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/clientes")]
    [Produces("application/json")]
    [Authorize(Policy = "Atendente")]
    public class ClientesController : BaseTenantController
    {
        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromServices] IConsultarClienteUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string busca = null)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ListarPaginadoAsync(tid, page, pageSize, busca));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> PorId(
            [FromServices] IConsultarClienteUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            var tid = RequireTenantId(ctx);
            var c = await useCase.PorIdAsync(tid, id);
            return c == null ? NotFound() : Ok(c);
        }

        [HttpPost]
        public async Task<IActionResult> Cadastrar(
            [FromServices] ICadastrarClienteUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromBody] ClienteInputModel input)
        {
            var tid = RequireTenantId(ctx);
            var c = await useCase.ExecuteAsync(tid, input);
            return CreatedAtAction(nameof(PorId), new { id = c.Id }, c);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(
            [FromServices] IAtualizarClienteUseCase useCase,
            [FromServices] ITenantContext ctx, int id,
            [FromBody] ClienteInputModel input)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid, id, input));
        }
    }
}

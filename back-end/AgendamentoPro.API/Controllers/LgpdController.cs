using AgendamentoPro.Application.Interfaces.Lgpd;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Route("api/admin/lgpd")]
    [Produces("application/json")]
    public class LgpdController : BaseTenantController
    {
        /// <summary>
        /// Exporta todos os dados pessoais de um cliente (direito de portabilidade LGPD).
        /// Retorna JSON serializável. Cliente pode pedir; admin executa.
        /// </summary>
        [HttpGet("clientes/{clienteId:int}/exportar")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Exportar(
            [FromServices] ILgpdUseCase useCase,
            [FromServices] ITenantContext ctx, int clienteId)
        {
            var tid = RequireTenantId(ctx);
            var dados = await useCase.ExportarDadosClienteAsync(tid, clienteId);
            Response.Headers["Content-Disposition"] = $"attachment; filename=cliente-{clienteId}-dados.json";
            return Ok(dados);
        }

        /// <summary>
        /// Anonimiza um cliente (direito ao esquecimento LGPD). Mantém histórico de
        /// agendamentos para integridade, mas sem identificação pessoal.
        /// </summary>
        [HttpPost("clientes/{clienteId:int}/anonimizar")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Anonimizar(
            [FromServices] ILgpdUseCase useCase,
            [FromServices] ITenantContext ctx, int clienteId)
        {
            var tid = RequireTenantId(ctx);
            await useCase.AnonimizarClienteAsync(tid, clienteId);
            return NoContent();
        }

        /// <summary>
        /// Anonimização em massa: clientes inativos há mais de N meses são anonimizados.
        /// Pode ser chamado via Hangfire mensal (não está agendado por default).
        /// </summary>
        [HttpPost("clientes/anonimizar-inativos")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> AnonimizarInativos(
            [FromServices] ILgpdUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] int inativoHaMeses = 24)
        {
            var tid = RequireTenantId(ctx);
            var n = await useCase.AnonimizarInativosAsync(tid, inativoHaMeses);
            return Ok(new { anonimizados = n, criterioMeses = inativoHaMeses });
        }
    }
}

using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Programa de fidelidade: cada agendamento concluído credita 10 pontos.
    /// 100 pontos = R$ 10 de cupom (uso único, válido por 60 dias).
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class FidelidadeController : BaseTenantController
    {
        public class TrocarPontosInput
        {
            public int ClienteId { get; set; }
            public int Pontos { get; set; }
        }

        [HttpGet("api/v1/admin/fidelidade/clientes/{clienteId:int}")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Saldo(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant, int clienteId)
        {
            var tid = RequireTenantId(tenant);
            var pts = await ctx.PontosFidelidade.AsNoTracking()
                .FirstOrDefaultAsync(p => p.R_TenId == tid && p.R_CliId == clienteId);
            return Ok(new { clienteId, saldo = pts?.PtsSaldo ?? 0 });
        }

        [HttpPost("api/v1/admin/fidelidade/trocar-por-cupom")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> TrocarPorCupom(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromServices] IUnitOfWork uow,
            [FromBody] TrocarPontosInput input)
        {
            var tid = RequireTenantId(tenant);
            var pts = await ctx.PontosFidelidade.FirstOrDefaultAsync(
                p => p.R_TenId == tid && p.R_CliId == input.ClienteId);
            if (pts == null || !pts.Debitar(input.Pontos))
                return BadRequest(new { message = "Saldo insuficiente." });

            // Cada 100 pts = R$ 10 fixo. Cupom de uso único, validade 60 dias.
            var valor = Math.Round(input.Pontos / 10m, 2);
            var codigo = $"FID-{input.ClienteId}-{DateTime.UtcNow.Ticks % 100000}";
            var cupom = new Cupom(tid, codigo, TipoDesconto.ValorFixo, valor,
                DateTime.UtcNow, DateTime.UtcNow.AddDays(60), usosMaximos: 1);
            ctx.Cupons.Add(cupom);
            await uow.SaveChangesAsync();
            return Ok(new { codigo = cupom.CupCodigo, valor, validoAte = cupom.CupValidoAte });
        }
    }
}

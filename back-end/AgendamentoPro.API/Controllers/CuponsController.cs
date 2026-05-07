using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class CuponsController : BaseTenantController
    {
        public class CupomInput
        {
            public string Codigo { get; set; }
            public TipoDesconto Tipo { get; set; } = TipoDesconto.Percentual;
            public decimal Valor { get; set; }
            public DateTime ValidoDe { get; set; }
            public DateTime ValidoAte { get; set; }
            public int UsosMaximos { get; set; }
        }

        // Admin: CRUD
        [HttpGet("api/v1/admin/cupons")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Listar(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant)
        {
            var tid = RequireTenantId(tenant);
            var lista = await ctx.Cupons.AsNoTracking()
                .Where(c => c.R_TenId == tid)
                .OrderByDescending(c => c.CupCriadoEm).ToListAsync();
            return Ok(lista);
        }

        [HttpPost("api/v1/admin/cupons")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Criar(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromServices] IUnitOfWork uow,
            [FromBody] CupomInput input)
        {
            var tid = RequireTenantId(tenant);
            var c = new Cupom(tid, input.Codigo, input.Tipo, input.Valor,
                input.ValidoDe, input.ValidoAte, input.UsosMaximos);
            ctx.Cupons.Add(c);
            await uow.SaveChangesAsync();
            return Ok(c);
        }

        [HttpPost("api/v1/admin/cupons/{id:int}/ativar")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> AlternarAtivo(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromServices] IUnitOfWork uow,
            int id, [FromQuery] bool ativo)
        {
            var tid = RequireTenantId(tenant);
            var c = await ctx.Cupons.FirstOrDefaultAsync(x => x.CupId == id && x.R_TenId == tid);
            if (c == null) return NotFound();
            if (ativo) c.Ativar(); else c.Desativar();
            await uow.SaveChangesAsync();
            return NoContent();
        }

        // Público: cliente final valida cupom no checkout
        [HttpGet("api/v1/t/{slug}/cupons/{codigo}/validar")]
        [AllowAnonymous]
        public async Task<IActionResult> Validar(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            string slug, string codigo,
            [FromQuery] decimal valorBase)
        {
            var tid = RequireTenantId(tenant);
            var c = await ctx.Cupons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.R_TenId == tid && x.CupCodigo == codigo.Trim().ToUpper());
            if (c == null || !c.EhValido(DateTime.UtcNow))
                return Ok(new { valido = false, message = "Cupom inválido ou expirado." });

            var novoValor = c.CalcularDesconto(valorBase);
            return Ok(new
            {
                valido = true,
                tipo = c.CupTipo.ToString(),
                valor = c.CupValor,
                valorBase,
                novoValor,
                economia = valorBase - novoValor
            });
        }
    }
}

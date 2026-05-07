using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Lista de espera: cliente que não achou horário entra na fila; admin vê
    /// e pode notificar quando vagar.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class ListaEsperaController : BaseTenantController
    {
        public class EntrarEsperaInput
        {
            public int ServicoId { get; set; }
            public DateTime DataDesejada { get; set; }
            public string ClienteNome { get; set; }
            public string ClienteTelefone { get; set; }
            public string ClienteEmail { get; set; }
            public string Observacao { get; set; }
        }

        // Público: cliente entra na espera
        [HttpPost("api/t/{slug}/lista-espera")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Entrar(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromServices] IUnitOfWork uow,
            string slug, [FromBody] EntrarEsperaInput input)
        {
            var tid = RequireTenantId(tenant);
            var item = new ListaEspera(tid, input.ServicoId, input.DataDesejada,
                input.ClienteNome, input.ClienteTelefone, input.ClienteEmail, input.Observacao);
            ctx.ListaEspera.Add(item);
            await uow.SaveChangesAsync();
            return Ok(new { id = item.LesId, posicao = await CalcularPosicao(ctx, tid, input.DataDesejada, item.LesId) });
        }

        // Admin: lista espera
        [HttpGet("api/admin/lista-espera")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Listar(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromQuery] DateTime? data = null,
            [FromQuery] bool somenteNaoNotificados = true)
        {
            var tid = RequireTenantId(tenant);
            var q = ctx.ListaEspera.AsNoTracking()
                .Include(l => l.Servico)
                .Where(l => l.R_TenId == tid);
            if (data.HasValue) q = q.Where(l => l.LesDataDesejada == data.Value.Date);
            if (somenteNaoNotificados) q = q.Where(l => !l.LesNotificado);

            var lista = await q.OrderBy(l => l.LesDataDesejada).ThenBy(l => l.LesCriadoEm).ToListAsync();
            return Ok(lista.Select(l => new
            {
                id = l.LesId,
                servicoId = l.R_SerId,
                servicoNome = l.Servico?.SerNome,
                dataDesejada = l.LesDataDesejada,
                cliente = new { l.LesClienteNome, l.LesClienteTelefone, l.LesClienteEmail },
                observacao = l.LesObservacao,
                notificado = l.LesNotificado,
                notificadoEm = l.LesNotificadoEm,
                criadoEm = l.LesCriadoEm
            }));
        }

        // Admin: marca como notificado
        [HttpPost("api/admin/lista-espera/{id:int}/notificar")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> MarcarNotificado(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromServices] IUnitOfWork uow, int id)
        {
            var tid = RequireTenantId(tenant);
            var item = await ctx.ListaEspera.FirstOrDefaultAsync(l => l.LesId == id && l.R_TenId == tid);
            if (item == null) return NotFound();
            item.MarcarNotificado();
            await uow.SaveChangesAsync();
            return NoContent();
        }

        private static async Task<int> CalcularPosicao(AgendamentoProDbContext ctx,
            int tenantId, DateTime data, int idAtual)
        {
            return 1 + await ctx.ListaEspera.CountAsync(l =>
                l.R_TenId == tenantId
                && l.LesDataDesejada == data.Date
                && !l.LesNotificado
                && l.LesId < idAtual);
        }
    }
}

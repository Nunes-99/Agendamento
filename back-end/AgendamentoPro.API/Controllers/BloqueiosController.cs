using AgendamentoPro.Core.Entities.Horarios;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// CRUD admin para bloqueios da agenda (feriados, recesso, manutenção).
    /// A entidade já era utilizada pelo DisponibilidadeService mas só era criada via banco.
    /// </summary>
    [ApiController]
    [Route("api/v1/admin/bloqueios")]
    [Authorize(Policy = "AdminTenant")]
    [Produces("application/json")]
    public class BloqueiosController : BaseTenantController
    {
        public class BloqueioInput
        {
            public int? RecursoId { get; set; }
            public DateTime DataInicio { get; set; }
            public DateTime DataFim { get; set; }
            public string Motivo { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Listar(
            [FromServices] IHorarioFuncionamentoRepository horarios,
            [FromServices] ITenantContext ctx,
            [FromQuery] DateTime? inicio = null, [FromQuery] DateTime? fim = null)
        {
            var tid = RequireTenantId(ctx);
            var de = inicio ?? DateTime.Today.AddDays(-30);
            var ate = fim ?? DateTime.Today.AddYears(1);
            var lista = await horarios.GetBloqueiosAsync(tid, de, ate);
            return Ok(lista.Select(b => new
            {
                id = b.BloId,
                recursoId = b.R_RecId,
                dataInicio = b.BloDataInicio,
                dataFim = b.BloDataFim,
                motivo = b.BloMotivo,
                criadoEm = b.BloCriadoEm
            }));
        }

        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromServices] IHorarioFuncionamentoRepository horarios,
            [FromServices] AgendamentoProDbContext db,
            [FromServices] ITenantContext ctx,
            [FromServices] IUnitOfWork uow,
            [FromBody] BloqueioInput input)
        {
            var tid = RequireTenantId(ctx);
            if (input.DataFim <= input.DataInicio)
                return BadRequest(new { message = "Data fim deve ser posterior à data início." });

            // CROSS-TENANT: bloqueio com R_RecId nulo é global ao tenant (todos os
            // recursos). Quando o admin especifica um RecursoId, validar que o
            // recurso pertence ao tenant — caso contrário admin do A poderia
            // bloquear recurso do tenant B.
            if (input.RecursoId.HasValue)
            {
                var existe = await db.Recursos.AnyAsync(r =>
                    r.RecId == input.RecursoId.Value && r.R_TenId == tid);
                if (!existe) return BadRequest(new { message = "Recurso inválido." });
            }

            var b = new BloqueioAgenda(tid, input.RecursoId, input.DataInicio, input.DataFim,
                input.Motivo ?? "Bloqueio");
            await horarios.CreateBloqueioAsync(b);
            await uow.SaveChangesAsync();
            return Ok(new { id = b.BloId });
        }
    }
}

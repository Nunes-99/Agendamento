using AgendamentoPro.Core.Entities.Agendamentos;
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
    /// Séries de agendamentos recorrentes (semanal, quinzenal, mensal).
    /// Cria N agendamentos individuais a partir de uma série mãe.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class RecorrenciasController : BaseTenantController
    {
        public class CriarRecorrenciaInput
        {
            public int ClienteId { get; set; }
            public int ServicoId { get; set; }
            public int RecursoId { get; set; }
            public DayOfWeek DiaSemana { get; set; }
            public TimeSpan HoraInicio { get; set; }
            public FrequenciaRecorrencia Frequencia { get; set; }
            public int Quantidade { get; set; }
            public DateTime DataInicio { get; set; }
        }

        [HttpPost("api/v1/admin/recorrencias")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Criar(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromServices] IAgendamentoRepository agendamentosRepo,
            [FromServices] IUnitOfWork uow,
            [FromBody] CriarRecorrenciaInput input)
        {
            var tid = RequireTenantId(tenant);
            var servico = await ctx.Servicos.FirstOrDefaultAsync(s => s.SerId == input.ServicoId && s.R_TenId == tid);
            if (servico == null) return BadRequest(new { message = "Serviço inválido." });
            var cliente = await ctx.Clientes.FirstOrDefaultAsync(c => c.CliId == input.ClienteId && c.R_TenId == tid);
            if (cliente == null) return BadRequest(new { message = "Cliente inválido." });
            // CROSS-TENANT: sem essa checagem, admin do tenant A poderia ocupar slot
            // do recurso do tenant B com a série inteira.
            var recurso = await ctx.Recursos.FirstOrDefaultAsync(r => r.RecId == input.RecursoId && r.R_TenId == tid);
            if (recurso == null) return BadRequest(new { message = "Recurso inválido." });
            if (!recurso.RecAtivo) return BadRequest(new { message = "Recurso inativo." });
            var tenantInfo = await ctx.Tenants.FirstOrDefaultAsync(t => t.TenId == tid);

            var rec = new AgendamentoRecorrente(tid, input.ClienteId, input.ServicoId, input.RecursoId,
                input.DiaSemana, input.HoraInicio, input.Frequencia, input.Quantidade, input.DataInicio);
            ctx.AgendamentosRecorrentes.Add(rec);
            await ctx.SaveChangesAsync();

            // Cria os N agendamentos individuais. Checa conflito ANTES de criar pra
            // distinguir "horário já ocupado" de outros erros e dar uma mensagem
            // específica. O índice único permanece como rede de segurança final.
            var horaFim = input.HoraInicio.Add(TimeSpan.FromMinutes(servico.SerDuracaoMinutos));
            var buffer = TimeSpan.FromMinutes(tenantInfo?.TenBufferMinutos ?? 0);
            var criados = new List<int>();
            var erros = new List<string>();
            foreach (var data in rec.GerarDatas())
            {
                try
                {
                    if (await agendamentosRepo.ExisteConflitoAsync(tid, input.RecursoId, data.Date,
                            input.HoraInicio.Subtract(buffer), horaFim.Add(buffer)))
                    {
                        erros.Add($"{data:dd/MM}: horário indisponível (conflito com outro agendamento).");
                        continue;
                    }
                    var ag = new Agendamento(tid, input.ClienteId, input.ServicoId, input.RecursoId,
                        data, input.HoraInicio, horaFim, servico.SerPreco,
                        tenantInfo?.TenPercentualEntrada ?? 20m,
                        $"Série recorrente #{rec.RecId}");
                    ag.ConfirmarPagamento(); // série recorrente é admin-criada, já confirma
                    ctx.Agendamentos.Add(ag);
                    await ctx.SaveChangesAsync();
                    criados.Add(ag.AgeId);
                }
                catch (Exception ex) { erros.Add($"{data:dd/MM}: {ex.Message}"); }
            }

            return Ok(new { recorrenciaId = rec.RecId, criados = criados.Count, ids = criados, erros });
        }

        [HttpGet("api/v1/admin/recorrencias")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Listar(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant)
        {
            var tid = RequireTenantId(tenant);
            var lista = await ctx.AgendamentosRecorrentes.AsNoTracking()
                .Where(r => r.R_TenId == tid && r.RecAtivo)
                .OrderByDescending(r => r.RecCriadoEm)
                .ToListAsync();
            return Ok(lista);
        }
    }
}

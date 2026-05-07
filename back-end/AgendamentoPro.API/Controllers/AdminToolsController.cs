using AgendamentoPro.Core.Entities.Common;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Ferramentas administrativas: visualização de log de auditoria, KPIs avançados,
    /// fechar caixa do dia, importação de clientes via CSV.
    /// </summary>
    [ApiController]
    [Route("api/admin/tools")]
    [Authorize(Policy = "AdminTenant")]
    [Produces("application/json")]
    public class AdminToolsController : BaseTenantController
    {
        // ===== Audit log: visualização paginada =====
        [HttpGet("auditoria")]
        public async Task<IActionResult> Auditoria(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
            [FromQuery] string tabela = null, [FromQuery] string acao = null,
            [FromQuery] DateTime? de = null, [FromQuery] DateTime? ate = null)
        {
            var tid = RequireTenantId(tenant);
            var q = ctx.LogsAuditoria.AsNoTracking().Where(l => l.R_TenId == tid);
            if (!string.IsNullOrEmpty(tabela)) q = q.Where(l => l.LogTabela == tabela);
            if (!string.IsNullOrEmpty(acao)) q = q.Where(l => l.LogAcao == acao);
            if (de.HasValue) q = q.Where(l => l.LogQuandoUtc >= de.Value);
            if (ate.HasValue) q = q.Where(l => l.LogQuandoUtc <= ate.Value);

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(l => l.LogQuandoUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(l => new
                {
                    id = l.LogId,
                    quando = l.LogQuandoUtc,
                    usuario = l.LogUsuarioEmail,
                    ip = l.LogIp,
                    correlationId = l.LogCorrelationId,
                    tabela = l.LogTabela,
                    chave = l.LogChave,
                    acao = l.LogAcao
                    // Payloads omitidos da listagem; cliente busca por id se quiser ver
                })
                .ToListAsync();
            return Ok(new { items, total, page, pageSize });
        }

        [HttpGet("auditoria/{id:int}")]
        public async Task<IActionResult> AuditoriaDetalhe(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant, int id)
        {
            var tid = RequireTenantId(tenant);
            var log = await ctx.LogsAuditoria.AsNoTracking()
                .FirstOrDefaultAsync(l => l.LogId == id && l.R_TenId == tid);
            return log == null ? NotFound() : Ok(log);
        }

        // ===== KPIs avançados =====
        [HttpGet("kpis")]
        public async Task<IActionResult> Kpis(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromQuery] DateTime? mesRef = null)
        {
            var tid = RequireTenantId(tenant);
            var refMes = (mesRef ?? DateTime.Today).Date;
            var inicioMes = new DateTime(refMes.Year, refMes.Month, 1);
            var fimMes = inicioMes.AddMonths(1).AddTicks(-1);
            var inicioAnterior = inicioMes.AddMonths(-1);
            var fimAnterior = inicioMes.AddTicks(-1);

            var atual = await ColetarKpis(ctx, tid, inicioMes, fimMes);
            var anterior = await ColetarKpis(ctx, tid, inicioAnterior, fimAnterior);

            return Ok(new
            {
                periodo = new { inicioMes, fimMes },
                atual,
                anterior,
                variacao = new
                {
                    receita = anterior.Receita == 0 ? (decimal?)null
                        : Math.Round((atual.Receita - anterior.Receita) / anterior.Receita * 100, 1),
                    agendamentos = anterior.Agendamentos == 0 ? (decimal?)null
                        : Math.Round(((decimal)(atual.Agendamentos - anterior.Agendamentos)) / anterior.Agendamentos * 100, 1)
                }
            });
        }

        // ===== Fechar caixa do dia =====
        [HttpGet("caixa")]
        public async Task<IActionResult> Caixa(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromQuery] DateTime? data = null)
        {
            var tid = RequireTenantId(tenant);
            var d = (data ?? DateTime.Today).Date;
            var ags = await ctx.Agendamentos.AsNoTracking()
                .Include(a => a.Pagamentos)
                .Where(a => a.R_TenId == tid && a.AgeData == d)
                .ToListAsync();

            var totalConcluidos = ags.Where(a => a.AgeStatus == StatusAgendamento.Concluido).Sum(a => a.AgeValorTotal);
            var totalRecebido = ags.SelectMany(a => a.Pagamentos)
                .Where(p => p.PagStatus == StatusPagamento.Aprovado)
                .Sum(p => p.PagValor);
            var pendentes = ags.Count(a => a.AgeStatus == StatusAgendamento.PendentePagamento
                || a.AgePagamentoStatus == StatusPagamento.Pendente);

            return Ok(new
            {
                data = d,
                totalAgendamentos = ags.Count,
                concluidos = ags.Count(a => a.AgeStatus == StatusAgendamento.Concluido),
                cancelados = ags.Count(a => a.AgeStatus == StatusAgendamento.Cancelado),
                noShow = ags.Count(a => a.AgeStatus == StatusAgendamento.NoShow),
                pendentes,
                receitaPrevista = ags.Sum(a => a.AgeValorTotal),
                receitaConcluida = totalConcluidos,
                receitaRecebida = totalRecebido
            });
        }

        // ===== Importação CSV de clientes =====
        public class ImportarClientesInput { public string CsvConteudo { get; set; } }

        [HttpPost("clientes/importar-csv")]
        public async Task<IActionResult> ImportarClientes(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromBody] ImportarClientesInput input)
        {
            var tid = RequireTenantId(tenant);
            if (string.IsNullOrWhiteSpace(input?.CsvConteudo))
                return BadRequest(new { message = "CSV vazio. Cabeçalho esperado: nome,telefone,email,cpf" });

            var linhas = input.CsvConteudo.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (linhas.Length < 2) return BadRequest(new { message = "Sem linhas de dados." });

            var header = linhas[0].Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
            int idxNome = Array.IndexOf(header, "nome");
            int idxTel = Array.IndexOf(header, "telefone");
            int idxEmail = Array.IndexOf(header, "email");
            int idxCpf = Array.IndexOf(header, "cpf");

            if (idxNome < 0) return BadRequest(new { message = "Coluna 'nome' obrigatória." });

            var inseridos = 0; var ignorados = 0; var erros = new List<string>();
            for (int i = 1; i < linhas.Length; i++)
            {
                var cols = linhas[i].Split(',');
                if (cols.Length <= idxNome) { ignorados++; continue; }
                var nome = cols[idxNome].Trim();
                if (string.IsNullOrEmpty(nome)) { ignorados++; continue; }
                var tel = idxTel >= 0 && idxTel < cols.Length ? cols[idxTel].Trim() : null;
                var email = idxEmail >= 0 && idxEmail < cols.Length ? cols[idxEmail].Trim() : null;
                var cpf = idxCpf >= 0 && idxCpf < cols.Length ? cols[idxCpf].Trim() : null;
                try
                {
                    var c = new Core.Entities.Clientes.Cliente(tid, nome, email, tel, tel, cpf);
                    ctx.Clientes.Add(c);
                    inseridos++;
                }
                catch (Exception ex) { erros.Add($"Linha {i + 1}: {ex.Message}"); }
            }
            await ctx.SaveChangesAsync();
            return Ok(new { inseridos, ignorados, erros });
        }

        private static async Task<KpiSnapshot> ColetarKpis(AgendamentoProDbContext ctx, int tid,
            DateTime inicio, DateTime fim)
        {
            var ags = await ctx.Agendamentos.AsNoTracking()
                .Where(a => a.R_TenId == tid && a.AgeData >= inicio && a.AgeData <= fim)
                .ToListAsync();
            var concluidos = ags.Where(a => a.AgeStatus == StatusAgendamento.Concluido).ToList();
            var cancelados = ags.Count(a => a.AgeStatus == StatusAgendamento.Cancelado);
            var noShow = ags.Count(a => a.AgeStatus == StatusAgendamento.NoShow);
            return new KpiSnapshot
            {
                Agendamentos = ags.Count,
                Concluidos = concluidos.Count,
                Cancelados = cancelados,
                NoShow = noShow,
                TaxaCancelamento = ags.Count > 0 ? Math.Round((decimal)cancelados / ags.Count * 100, 1) : 0,
                TaxaNoShow = ags.Count > 0 ? Math.Round((decimal)noShow / ags.Count * 100, 1) : 0,
                Receita = concluidos.Sum(a => a.AgeValorTotal),
                TicketMedio = concluidos.Count > 0 ? Math.Round(concluidos.Average(a => a.AgeValorTotal), 2) : 0
            };
        }

        private class KpiSnapshot
        {
            public int Agendamentos { get; set; }
            public int Concluidos { get; set; }
            public int Cancelados { get; set; }
            public int NoShow { get; set; }
            public decimal TaxaCancelamento { get; set; }
            public decimal TaxaNoShow { get; set; }
            public decimal Receita { get; set; }
            public decimal TicketMedio { get; set; }
        }
    }
}

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
    [Route("api/v1/admin/tools")]
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

            // Cancelados/no-show ficam fora dos números "vivos" do caixa: um dia cheio de
            // cancelamentos mostrava a receita cheia em "prevista" e o pagamento pendente
            // do agendamento cancelado ainda contava como "pendente".
            var validos = ags.Where(a => a.AgeStatus != StatusAgendamento.Cancelado
                                      && a.AgeStatus != StatusAgendamento.NoShow).ToList();

            var totalConcluidos = validos.Where(a => a.AgeStatus == StatusAgendamento.Concluido).Sum(a => a.AgeValorTotal);
            var totalRecebido = ags.SelectMany(a => a.Pagamentos)
                .Where(p => p.PagStatus == StatusPagamento.Aprovado)
                .Sum(p => p.PagValor);
            var pendentes = validos.Count(a => a.AgeStatus == StatusAgendamento.PendentePagamento
                || a.AgePagamentoStatus == StatusPagamento.Pendente);

            return Ok(new
            {
                data = d,
                totalAgendamentos = validos.Count,
                concluidos = validos.Count(a => a.AgeStatus == StatusAgendamento.Concluido),
                cancelados = ags.Count(a => a.AgeStatus == StatusAgendamento.Cancelado),
                noShow = ags.Count(a => a.AgeStatus == StatusAgendamento.NoShow),
                pendentes,
                receitaPrevista = validos.Sum(a => a.AgeValorTotal),
                receitaConcluida = totalConcluidos,
                receitaRecebida = totalRecebido
            });
        }

        // ===== Importação CSV de clientes =====
        public class ImportarClientesInput { public string CsvConteudo { get; set; } }

        /// <summary>
        /// Limite de 2 MB no payload (~ 20-30k clientes). Acima disso o admin deveria
        /// dividir o arquivo. Sem esse limite, payload arbitrário derruba o processo
        /// por OOM (StringReader + entities tracked em memória).
        /// </summary>
        private const int CsvMaxBytes = 2 * 1024 * 1024;

        [HttpPost("clientes/importar-csv")]
        public async Task<IActionResult> ImportarClientes(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromBody] ImportarClientesInput input)
        {
            var tid = RequireTenantId(tenant);
            if (string.IsNullOrWhiteSpace(input?.CsvConteudo))
                return BadRequest(new { message = "CSV vazio. Cabeçalho esperado: nome,telefone,email,cpf" });

            if (input.CsvConteudo.Length > CsvMaxBytes)
                return BadRequest(new
                {
                    message = $"CSV excede o tamanho máximo ({CsvMaxBytes / 1024 / 1024} MB). Divida em arquivos menores."
                });

            // Pré-carrega telefones e emails existentes do tenant para deduplicar.
            // Usa lower-case + dígitos para comparação (ignora máscara do telefone).
            var existentes = await ctx.Clientes.AsNoTracking()
                .Where(c => c.R_TenId == tid)
                .Select(c => new { c.CliTelefone, c.CliEmail })
                .ToListAsync();
            var telefonesUsados = new HashSet<string>(
                existentes.Select(e => NormalizarTelefone(e.CliTelefone))
                    .Where(t => !string.IsNullOrEmpty(t)));
            var emailsUsados = new HashSet<string>(
                existentes.Select(e => (e.CliEmail ?? "").Trim().ToLowerInvariant())
                    .Where(e => !string.IsNullOrEmpty(e)));

            // Usa CsvHelper: trata aspas, escapes, encoding UTF-8 com BOM, etc.
            var inseridos = 0; var ignorados = 0; var duplicados = 0;
            var erros = new List<string>();
            try
            {
                using var reader = new StringReader(input.CsvConteudo);
                using var csv = new CsvHelper.CsvReader(reader,
                    new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                    {
                        Delimiter = ",",
                        TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                        HeaderValidated = null,
                        MissingFieldFound = null,
                        BadDataFound = null
                    });
                await csv.ReadAsync();
                csv.ReadHeader();

                int linha = 1;
                while (await csv.ReadAsync())
                {
                    linha++;
                    string nome = TryGet(csv, "nome");
                    if (string.IsNullOrEmpty(nome)) { ignorados++; continue; }
                    var tel = TryGet(csv, "telefone");
                    var email = TryGet(csv, "email");
                    var cpf = TryGet(csv, "cpf");

                    var telNorm = NormalizarTelefone(tel);
                    var emailNorm = (email ?? "").Trim().ToLowerInvariant();

                    // Dedup: contra existentes no banco E contra outras linhas já processadas
                    // neste mesmo CSV. Usa telefone OU email como chave de unicidade.
                    if ((!string.IsNullOrEmpty(telNorm) && !telefonesUsados.Add(telNorm)) ||
                        (!string.IsNullOrEmpty(emailNorm) && !emailsUsados.Add(emailNorm)))
                    {
                        duplicados++;
                        continue;
                    }

                    try
                    {
                        var c = new Core.Entities.Clientes.Cliente(tid, nome, email, tel, tel, cpf);
                        ctx.Clientes.Add(c);
                        inseridos++;
                    }
                    catch (Exception ex) { erros.Add($"Linha {linha}: {ex.Message}"); }
                }
                await ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "CSV inválido: " + ex.Message });
            }
            return Ok(new { inseridos, ignorados, duplicados, erros });
        }

        private static string NormalizarTelefone(string raw) =>
            string.IsNullOrEmpty(raw) ? string.Empty
                : new string(raw.Where(char.IsDigit).ToArray());

        private static string TryGet(CsvHelper.CsvReader csv, string nome)
        {
            try { return csv.GetField<string>(nome)?.Trim(); }
            catch { return null; }
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

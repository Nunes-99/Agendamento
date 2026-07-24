using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Endpoints da área "Minha Conta" do cliente final (B2C).
    /// Autenticação via OTP por WhatsApp (ver <see cref="OtpController"/>),
    /// JWT com role=Cliente e claim clienteId.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "Cliente")]
    [Produces("application/json")]
    public class MinhaContaController : BaseTenantController
    {
        private static int RequireClienteId(ClaimsPrincipal user)
        {
            var raw = user.FindFirst("clienteId")?.Value;
            if (!int.TryParse(raw, out var id))
                throw new UnauthorizedAccessException("Token de cliente inválido.");
            return id;
        }

        /// <summary>Retorna nome, telefone, e-mail do cliente autenticado.</summary>
        [HttpGet("api/v1/t/{slug}/minha-conta")]
        public async Task<IActionResult> Meu(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant, string slug)
        {
            var tid = RequireTenantId(tenant);
            var cid = RequireClienteId(User);
            var c = await ctx.Clientes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CliId == cid && x.R_TenId == tid);
            if (c == null) return NotFound();
            return Ok(new
            {
                id = c.CliId,
                nome = c.CliNome,
                telefone = c.CliTelefone,
                email = c.CliEmail
            });
        }

        public class AtualizarPerfilInput { public string Nome { get; set; } public string Email { get; set; } }

        /// <summary>Permite ao cliente atualizar nome e e-mail.</summary>
        [HttpPut("api/v1/t/{slug}/minha-conta")]
        public async Task<IActionResult> AtualizarPerfil(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            string slug, [FromBody] AtualizarPerfilInput input)
        {
            var tid = RequireTenantId(tenant);
            var cid = RequireClienteId(User);
            var c = await ctx.Clientes.FirstOrDefaultAsync(x => x.CliId == cid && x.R_TenId == tid);
            if (c == null) return NotFound();
            var novoNome = string.IsNullOrWhiteSpace(input.Nome) ? c.CliNome : input.Nome.Trim();
            var novoEmail = input.Email == null ? c.CliEmail : input.Email.Trim();
            c.Atualizar(novoNome, novoEmail, c.CliTelefone, c.CliWhatsApp, c.CliCpf, c.CliObservacao);
            await ctx.SaveChangesAsync();
            return Ok(new { id = c.CliId, nome = c.CliNome, email = c.CliEmail });
        }

        /// <summary>Lista agendamentos futuros e passados do cliente autenticado.</summary>
        [HttpGet("api/v1/t/{slug}/minha-conta/agendamentos")]
        public async Task<IActionResult> Agendamentos(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant, string slug)
        {
            var tid = RequireTenantId(tenant);
            var cid = RequireClienteId(User);
            // O desempate por hora é feito EM MEMÓRIA: o SQLite não ordena por
            // TimeSpan e a consulta inteira estourava 500 — o cliente entrava na
            // conta dele e não via agendamento nenhum. Mesmo motivo do comentário
            // em AgendamentoRepository.GetPorPeriodoAsync.
            //
            // O Take(50) fica DEPOIS da ordenação completa, senão o corte poderia
            // pegar as 50 linhas erradas dentro de um mesmo dia.
            var doCliente = await ctx.Agendamentos.AsNoTracking()
                .Include(a => a.Servico)
                .Where(a => a.R_TenId == tid && a.R_CliId == cid)
                .OrderByDescending(a => a.AgeData)
                .ToListAsync();

            var lista = doCliente
                .OrderByDescending(a => a.AgeData).ThenByDescending(a => a.AgeHoraInicio)
                .Take(50)
                .Select(a => new
                {
                    id = a.AgeId,
                    data = a.AgeData,
                    horaInicio = a.AgeHoraInicio,
                    horaFim = a.AgeHoraFim,
                    servicoNome = a.Servico.SerNome,
                    status = (int)a.AgeStatus,
                    statusPagamento = (int)a.AgePagamentoStatus,
                    valorTotal = a.AgeValorTotal,
                    tokenSelfService = a.AgeAcessoToken
                })
                .ToList();
            return Ok(lista);
        }

        /// <summary>Lista pacotes pré-pagos ativos do cliente (com saldo restante).</summary>
        [HttpGet("api/v1/t/{slug}/minha-conta/pacotes")]
        public async Task<IActionResult> Pacotes(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant, string slug)
        {
            var tid = RequireTenantId(tenant);
            var cid = RequireClienteId(User);
            var lista = await ctx.SaldosPacote.AsNoTracking()
                .Include(s => s.Pacote).ThenInclude(p => p.Servico)
                .Where(s => s.R_TenId == tid && s.R_CliId == cid)
                .OrderByDescending(s => s.SaldCriadoEm)
                .Select(s => new
                {
                    id = s.SaldId,
                    pacoteNome = s.Pacote.PctNome,
                    servicoNome = s.Pacote.Servico.SerNome,
                    quantidadeRestante = s.SaldQuantidadeRestante,
                    quantidadeOriginal = s.Pacote.PctQuantidade,
                    expiraEm = s.SaldExpiraEm,
                    status = s.SaldStatus.ToString().ToLowerInvariant()
                })
                .ToListAsync();
            return Ok(lista);
        }

        /// <summary>Saldo de pontos no programa de fidelidade.</summary>
        [HttpGet("api/v1/t/{slug}/minha-conta/fidelidade")]
        public async Task<IActionResult> Fidelidade(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant, string slug)
        {
            var tid = RequireTenantId(tenant);
            var cid = RequireClienteId(User);
            var pts = await ctx.PontosFidelidade.AsNoTracking()
                .FirstOrDefaultAsync(p => p.R_TenId == tid && p.R_CliId == cid);
            return Ok(new { saldo = pts?.PtsSaldo ?? 0 });
        }
    }
}

using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Services;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Endpoints das 3 features de negócio: Recorrência, Pacotes pré-pagos e Fidelidade.
    /// CRUD enxuto - foco em deixar o fluxo funcional.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class NegocioController : BaseTenantController
    {
        // ============== Recorrência ==============

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
        public async Task<IActionResult> CriarRecorrencia(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromServices] IUnitOfWork uow,
            [FromBody] CriarRecorrenciaInput input)
        {
            var tid = RequireTenantId(tenant);
            var servico = await ctx.Servicos.FirstOrDefaultAsync(s => s.SerId == input.ServicoId && s.R_TenId == tid);
            if (servico == null) return BadRequest(new { message = "Serviço inválido." });
            var cliente = await ctx.Clientes.FirstOrDefaultAsync(c => c.CliId == input.ClienteId && c.R_TenId == tid);
            if (cliente == null) return BadRequest(new { message = "Cliente inválido." });
            var tenantInfo = await ctx.Tenants.FirstOrDefaultAsync(t => t.TenId == tid);

            var rec = new AgendamentoRecorrente(tid, input.ClienteId, input.ServicoId, input.RecursoId,
                input.DiaSemana, input.HoraInicio, input.Frequencia, input.Quantidade, input.DataInicio);
            ctx.AgendamentosRecorrentes.Add(rec);
            await ctx.SaveChangesAsync();

            // Cria os N agendamentos individuais
            var horaFim = input.HoraInicio.Add(TimeSpan.FromMinutes(servico.SerDuracaoMinutos));
            var criados = new List<int>();
            var erros = new List<string>();
            foreach (var data in rec.GerarDatas())
            {
                try
                {
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
        public async Task<IActionResult> ListarRecorrencias(
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

        // ============== Pacotes pré-pagos ==============

        public class CriarPacoteInput
        {
            public int ServicoId { get; set; }
            public string Nome { get; set; }
            public int Quantidade { get; set; }
            public decimal Preco { get; set; }
            public int ValidadeDias { get; set; }
        }

        [HttpGet("api/v1/admin/pacotes")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> ListarPacotes(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant)
        {
            var tid = RequireTenantId(tenant);
            var lista = await ctx.PacotesPrePagos.AsNoTracking()
                .Include(p => p.Servico)
                .Where(p => p.R_TenId == tid && !p.Excluido && p.PctAtivo)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpPost("api/v1/admin/pacotes")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> CriarPacote(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromServices] IUnitOfWork uow,
            [FromBody] CriarPacoteInput input)
        {
            var tid = RequireTenantId(tenant);
            var pacote = new PacotePrePago(tid, input.ServicoId, input.Nome,
                input.Quantidade, input.Preco, input.ValidadeDias);
            ctx.PacotesPrePagos.Add(pacote);
            await uow.SaveChangesAsync();
            return Ok(pacote);
        }

        [HttpPost("api/v1/t/{slug}/pacotes/{pacoteId:int}/comprar")]
        [AllowAnonymous]
        public async Task<IActionResult> ComprarPacote(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromServices] IUnitOfWork uow,
            [FromServices] IEnumerable<IGatewayPagamento> gateways,
            string slug, int pacoteId, [FromBody] ClientePublicoInputModel cliente)
        {
            var tid = RequireTenantId(tenant);
            var pacote = await ctx.PacotesPrePagos.FirstOrDefaultAsync(p => p.PctId == pacoteId
                && p.R_TenId == tid && p.PctAtivo);
            if (pacote == null) return NotFound();

            Cliente cli = null;
            if (!string.IsNullOrEmpty(cliente.Telefone))
                cli = await ctx.Clientes.FirstOrDefaultAsync(c => c.R_TenId == tid && c.CliTelefone == cliente.Telefone);
            if (cli == null)
            {
                cli = new Cliente(tid, cliente.Nome, cliente.Email, cliente.Telefone, cliente.WhatsApp, cliente.Cpf);
                ctx.Clientes.Add(cli);
                await uow.SaveChangesAsync();
            }

            var saldo = new SaldoPacote(tid, cli.CliId, pacote);
            ctx.SaldosPacote.Add(saldo);
            await uow.SaveChangesAsync();

            // Cria cobrança no gateway PIX. Saldo fica pendente até webhook aprovar.
            var gateway = gateways.FirstOrDefault();
            if (gateway == null)
                return StatusCode(500, new { message = "Gateway de pagamento não configurado." });

            var cobranca = await gateway.CriarCobrancaAsync(tid, agendamentoId: 0,
                pacote.PctPreco, FormaPagamento.Pix,
                $"Pacote: {pacote.PctNome}", expiracaoMinutos: 30);

            saldo.DefinirGatewayId(cobranca.GatewayId);
            ctx.SaldosPacote.Update(saldo);
            await uow.SaveChangesAsync();

            return Ok(new
            {
                saldoPacoteId = saldo.SaldId,
                qrCode = cobranca.QrCode,
                linkPagamento = cobranca.LinkPagamento,
                valor = pacote.PctPreco,
                expiracao = cobranca.Expiracao
            });
        }

        [HttpGet("api/v1/t/{slug}/saldos-pacote/{saldoId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> ConsultarSaldoPacote(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            string slug, int saldoId)
        {
            var tid = RequireTenantId(tenant);
            var saldo = await ctx.SaldosPacote.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SaldId == saldoId && s.R_TenId == tid);
            if (saldo == null) return NotFound();
            return Ok(new
            {
                saldoPacoteId = saldo.SaldId,
                status = saldo.SaldStatus.ToString().ToLowerInvariant(),
                restante = saldo.SaldQuantidadeRestante
            });
        }

        [HttpGet("api/v1/t/{slug}/pacotes")]
        [AllowAnonymous]
        public async Task<IActionResult> ListarPacotesPublico(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant, string slug)
        {
            var tid = RequireTenantId(tenant);
            var lista = await ctx.PacotesPrePagos.AsNoTracking()
                .Where(p => p.R_TenId == tid && !p.Excluido && p.PctAtivo)
                .ToListAsync();
            return Ok(lista);
        }

        // ============== Fidelidade ==============

        [HttpGet("api/v1/admin/fidelidade/clientes/{clienteId:int}")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> SaldoPontos(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant, int clienteId)
        {
            var tid = RequireTenantId(tenant);
            var pts = await ctx.PontosFidelidade.AsNoTracking()
                .FirstOrDefaultAsync(p => p.R_TenId == tid && p.R_CliId == clienteId);
            return Ok(new { clienteId, saldo = pts?.PtsSaldo ?? 0 });
        }

        public class TrocarPontosInput { public int ClienteId { get; set; } public int Pontos { get; set; } }

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

            // Cada 100 pts = R$ 10 fixo (ajustável). Cria cupom de uso único válido por 60 dias.
            var valor = Math.Round(input.Pontos / 10m, 2);
            var codigo = $"FID-{input.ClienteId}-{DateTime.UtcNow.Ticks % 100000}";
            var cupom = new Cupom(tid, codigo, TipoDesconto.ValorFixo, valor,
                DateTime.UtcNow, DateTime.UtcNow.AddDays(60), usosMaximos: 1);
            ctx.Cupons.Add(cupom);
            await uow.SaveChangesAsync();
            return Ok(new { codigo = cupom.CupCodigo, valor = valor, validoAte = cupom.CupValidoAte });
        }
    }
}

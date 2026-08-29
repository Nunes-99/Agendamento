using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Services;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Pacotes pré-pagos: cliente compra N atendimentos do mesmo serviço com desconto.
    /// Pago upfront via PIX. Saldo fica Pendente até o webhook do gateway aprovar → Ativo.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class PacotesController : BaseTenantController
    {
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
        public async Task<IActionResult> ListarAdmin(
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
        public async Task<IActionResult> Criar(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromServices] IUnitOfWork uow,
            [FromBody] CriarPacoteInput input)
        {
            var tid = RequireTenantId(tenant);
            // CROSS-TENANT: garantir que o serviço pertence ao mesmo tenant — sem
            // isso, admin do A poderia criar pacote referenciando serviço do B.
            var servico = await ctx.Servicos.FirstOrDefaultAsync(
                s => s.SerId == input.ServicoId && s.R_TenId == tid);
            if (servico == null) return BadRequest(new { message = "Serviço inválido." });

            var pacote = new PacotePrePago(tid, input.ServicoId, input.Nome,
                input.Quantidade, input.Preco, input.ValidadeDias);
            ctx.PacotesPrePagos.Add(pacote);
            await uow.SaveChangesAsync();
            return Ok(pacote);
        }

        [HttpGet("api/v1/t/{slug}/pacotes")]
        [AllowAnonymous]
        public async Task<IActionResult> ListarPublico(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant, string slug)
        {
            var tid = RequireTenantId(tenant);
            var lista = await ctx.PacotesPrePagos.AsNoTracking()
                .Where(p => p.R_TenId == tid && !p.Excluido && p.PctAtivo)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpPost("api/v1/t/{slug}/pacotes/{pacoteId:int}/comprar")]
        [AllowAnonymous]
        // 5/min/IP — endpoint cria SaldoPacote e dispara cobrança no gateway.
        // Sem rate-limit dedicado, atacante anônimo pode disparar N cobranças
        // (poluição no painel do gateway, consumo de quota MP).
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Comprar(
            [FromServices] AgendamentoProDbContext ctx,
            [FromServices] ITenantContext tenant,
            [FromServices] IUnitOfWork uow,
            [FromServices] IEnumerable<IGatewayPagamento> gateways,
            [FromServices] Core.Interfaces.Database.Repositories.IClienteRepository clientes,
            string slug, int pacoteId, [FromBody] ClientePublicoInputModel cliente)
        {
            var tid = RequireTenantId(tenant);
            var pacote = await ctx.PacotesPrePagos.FirstOrDefaultAsync(p => p.PctId == pacoteId
                && p.R_TenId == tid && p.PctAtivo);
            if (pacote == null) return NotFound();

            // Busca via repositório: compara telefone NORMALIZADO (máscaras diferentes
            // entre fluxos duplicavam o cliente).
            Cliente cli = null;
            if (!string.IsNullOrEmpty(cliente.Telefone))
                cli = await clientes.GetByTelefoneAsync(tid, cliente.Telefone);

            // Pacote é cobrado em PIX upfront. Sem Suporta(Pix), se Stripe estiver
            // registrado antes de MP, FirstOrDefault() pegaria Stripe (que não suporta
            // PIX) e quebraria o endpoint. A checagem vem ANTES de persistir qualquer
            // coisa: sem gateway, criar cliente + SaldoPacote deixava um saldo órfão
            // pendente — e o 500 genérico escondia o motivo do cliente final.
            var gateway = gateways.FirstOrDefault(g => g.Suporta(FormaPagamento.Pix));
            if (gateway == null)
                return StatusCode(503, new
                {
                    message = "O pagamento online está indisponível neste estabelecimento no momento. "
                        + "Entre em contato para comprar o pacote."
                });

            if (cli == null)
            {
                cli = new Cliente(tid, cliente.Nome, cliente.Email, cliente.Telefone, cliente.WhatsApp, cliente.Cpf);
                ctx.Clientes.Add(cli);
                await uow.SaveChangesAsync();
            }

            var saldo = new SaldoPacote(tid, cli.CliId, pacote);
            ctx.SaldosPacote.Add(saldo);
            await uow.SaveChangesAsync();

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
        public async Task<IActionResult> ConsultarSaldo(
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
    }
}

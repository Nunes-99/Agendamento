using AgendamentoPro.Core.Entities.Notificacoes;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Infrastructure.Services.WhatsApp
{
    /// <summary>
    /// Job Hangfire que envia lembretes 24h e 2h antes do agendamento.
    ///
    /// Em modo Shared (default): roda 1 vez, varrendo todos os agendamentos
    /// no banco compartilhado.
    ///
    /// Em modo PerTenant: itera os tenants (lidos do banco SHARED, que continua
    /// sendo a fonte de verdade pro registro de tenants), e para cada um abre
    /// um scope com TenantContext setado, fazendo o DbContext daquele scope
    /// resolver a connection para o banco específico do tenant.
    ///
    /// Idempotente: chave por (agendamento, tipo) na tabela Notificacao.
    /// </summary>
    public class LembreteJob
    {
        private static readonly TimeSpan Janela = TimeSpan.FromMinutes(10);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantConnectionFactory _connFactory;
        private readonly ILogger<LembreteJob> _logger;

        public LembreteJob(IServiceScopeFactory scopeFactory,
            ITenantConnectionFactory connFactory, ILogger<LembreteJob> logger)
        {
            _scopeFactory = scopeFactory;
            _connFactory = connFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
        public async Task ExecutarAsync(CancellationToken ct)
        {
            if (!_connFactory.IsPerTenant)
            {
                // Modo Shared: roda direto com um scope único.
                await ExecutarParaTenantAsync(tenantId: null, ct);
                return;
            }

            // Modo PerTenant: lista tenants ativos via DB shared e roda para cada um
            // num scope separado (TenantContext setado via wrapper inline).
            List<int> tenantsAtivos;
            using (var scopeShared = _scopeFactory.CreateScope())
            {
                var tenants = scopeShared.ServiceProvider.GetRequiredService<ITenantRepository>();
                tenantsAtivos = (await tenants.GetAllAsync())
                    .Where(t => t.TenAtivo)
                    .Select(t => t.TenId)
                    .ToList();
            }

            foreach (var tid in tenantsAtivos)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await ExecutarParaTenantAsync(tid, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lembretes do tenant {Id} falharam, continuando com os próximos.", tid);
                }
            }
        }

        private async Task ExecutarParaTenantAsync(int? tenantId, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            // Em PerTenant, força o TenantContext do scope antes de resolver DbContext.
            if (tenantId.HasValue)
            {
                var tCtx = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                tCtx.SetTenant(tenantId.Value, slug: null);
            }

            var ctx = scope.ServiceProvider.GetRequiredService<AgendamentoProDbContext>();
            var whatsapp = scope.ServiceProvider.GetRequiredService<INotificadorWhatsApp>();
            if (!whatsapp.Ativo)
            {
                _logger.LogDebug("LembreteJob (tenant {Tid}): WhatsApp inativo, ignorado.", tenantId);
                return;
            }

            var agora = DateTime.UtcNow;
            await DispararAsync(ctx, whatsapp, "Lembrete24h", agora.AddHours(24), ct);
            await DispararAsync(ctx, whatsapp, "Lembrete2h", agora.AddHours(2), ct);
        }

        private async Task DispararAsync(AgendamentoProDbContext ctx, INotificadorWhatsApp whatsapp,
            string tipo, DateTime alvo, CancellationToken ct)
        {
            var inicio = alvo.AddMinutes(-Janela.TotalMinutes);
            var fim = alvo.AddMinutes(Janela.TotalMinutes);
            var dataAlvo = alvo.Date;

            var candidatos = await ctx.Agendamentos
                .Include(a => a.Cliente)
                .Include(a => a.Servico)
                .Include(a => a.Tenant)
                .Where(a => a.AgeStatus == StatusAgendamento.Confirmado
                    && a.AgeData >= dataAlvo.AddDays(-1) && a.AgeData <= dataAlvo.AddDays(1))
                .ToListAsync(ct);

            foreach (var ag in candidatos)
            {
                ct.ThrowIfCancellationRequested();
                var dataHora = ag.AgeData.Date.Add(ag.AgeHoraInicio);
                if (dataHora < inicio || dataHora > fim) continue;

                var jaEnviou = await ctx.Notificacoes.AnyAsync(n =>
                    n.R_AgeId == ag.AgeId && n.NotTipo == tipo && n.NotStatus == "Enviada", ct);
                if (jaEnviou) continue;

                var numero = ag.Cliente?.CliWhatsApp ?? ag.Cliente?.CliTelefone;
                if (string.IsNullOrWhiteSpace(numero)) continue;

                var notificacao = new Notificacao(ag.R_TenId, ag.AgeId,
                    canal: "WhatsApp", tipo: tipo, destinatario: numero,
                    mensagem: $"{tipo} - {ag.Servico?.SerNome} em {dataHora:dd/MM HH:mm}");
                ctx.Notificacoes.Add(notificacao);
                await ctx.SaveChangesAsync(ct);

                try
                {
                    if (tipo == "Lembrete24h")
                    {
                        await whatsapp.EnviarTemplateAsync(numero, "lembrete_24h", "pt_BR",
                            ag.Cliente?.CliNome ?? "Cliente",
                            ag.Servico?.SerNome ?? "serviço",
                            dataHora.ToString("dd/MM/yyyy"),
                            dataHora.ToString("HH:mm"),
                            ag.Tenant?.TenNome ?? "");
                    }
                    else
                    {
                        await whatsapp.EnviarTemplateAsync(numero, "lembrete_2h", "pt_BR",
                            ag.Cliente?.CliNome ?? "Cliente",
                            ag.Servico?.SerNome ?? "serviço",
                            dataHora.ToString("HH:mm"));
                    }
                    notificacao.MarcarEnviada();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar {Tipo} para agendamento {Id}", tipo, ag.AgeId);
                    notificacao.MarcarErro(ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message);
                }
                ctx.Notificacoes.Update(notificacao);
                await ctx.SaveChangesAsync(ct);
            }
        }
    }
}

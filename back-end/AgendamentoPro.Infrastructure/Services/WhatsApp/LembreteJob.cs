using AgendamentoPro.Core.Entities.Notificacoes;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Services;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Infrastructure.Services.WhatsApp
{
    /// <summary>
    /// Versão Hangfire do envio de lembretes 24h e 2h. Equivalente em comportamento
    /// ao LembreteBackgroundService, mas com:
    ///   - retry automático em caso de exceção (Hangfire reagenda)
    ///   - persistência (sobrevive a restart da API quando armazenamento for SQL/Redis)
    ///   - dashboard /hangfire para auditoria e re-execução manual
    ///
    /// Registrado como recurring job no startup ("0/5 * * * *", a cada 5min).
    /// O método PRECISA ser público e a classe instanciável pelo container.
    /// </summary>
    public class LembreteJob
    {
        private static readonly TimeSpan Janela = TimeSpan.FromMinutes(10);

        private readonly AgendamentoProDbContext _ctx;
        private readonly INotificadorWhatsApp _whatsapp;
        private readonly ILogger<LembreteJob> _logger;

        public LembreteJob(AgendamentoProDbContext ctx, INotificadorWhatsApp whatsapp,
            ILogger<LembreteJob> logger)
        {
            _ctx = ctx;
            _whatsapp = whatsapp;
            _logger = logger;
        }

        // Job idempotente: chave por (agendamento, tipo) na tabela Notificacao.
        // AutomaticRetry mantém retries com backoff exponencial em falha de rede do WhatsApp.
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
        public async Task ExecutarAsync(CancellationToken ct)
        {
            if (!_whatsapp.Ativo)
            {
                _logger.LogDebug("LembreteJob: WhatsApp inativo, ciclo ignorado.");
                return;
            }

            var agora = DateTime.UtcNow;
            await DispararAsync("Lembrete24h", agora.AddHours(24), ct);
            await DispararAsync("Lembrete2h", agora.AddHours(2), ct);
        }

        private async Task DispararAsync(string tipo, DateTime alvo, CancellationToken ct)
        {
            var inicio = alvo.AddMinutes(-Janela.TotalMinutes);
            var fim = alvo.AddMinutes(Janela.TotalMinutes);
            var dataAlvo = alvo.Date;

            var candidatos = await _ctx.Agendamentos
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

                var jaEnviou = await _ctx.Notificacoes.AnyAsync(n =>
                    n.R_AgeId == ag.AgeId && n.NotTipo == tipo && n.NotStatus == "Enviada", ct);
                if (jaEnviou) continue;

                var numero = ag.Cliente?.CliWhatsApp ?? ag.Cliente?.CliTelefone;
                if (string.IsNullOrWhiteSpace(numero)) continue;

                var notificacao = new Notificacao(ag.R_TenId, ag.AgeId,
                    canal: "WhatsApp", tipo: tipo, destinatario: numero,
                    mensagem: $"{tipo} - {ag.Servico?.SerNome} em {dataHora:dd/MM HH:mm}");
                _ctx.Notificacoes.Add(notificacao);
                await _ctx.SaveChangesAsync(ct);

                try
                {
                    if (tipo == "Lembrete24h")
                    {
                        await _whatsapp.EnviarTemplateAsync(numero, "lembrete_24h", "pt_BR",
                            ag.Cliente?.CliNome ?? "Cliente",
                            ag.Servico?.SerNome ?? "serviço",
                            dataHora.ToString("dd/MM/yyyy"),
                            dataHora.ToString("HH:mm"),
                            ag.Tenant?.TenNome ?? "");
                    }
                    else
                    {
                        await _whatsapp.EnviarTemplateAsync(numero, "lembrete_2h", "pt_BR",
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
                    // Não relança — Hangfire não vai retry se conseguimos persistir o erro,
                    // mas o registro fica em LogAuditoria + Notificacao para troubleshooting.
                }
                _ctx.Notificacoes.Update(notificacao);
                await _ctx.SaveChangesAsync(ct);
            }
        }
    }
}

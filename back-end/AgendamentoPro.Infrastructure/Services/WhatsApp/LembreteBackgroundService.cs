using AgendamentoPro.Core.Entities.Notificacoes;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Services;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Infrastructure.Services.WhatsApp
{
    /// <summary>
    /// Envia lembretes de agendamento via WhatsApp 24h e 2h antes do horário marcado.
    /// Roda como BackgroundService - intervalo configurável (default 5 min).
    /// Usa a tabela Notificacao como ledger para garantir idempotência: cada
    /// (agendamento, tipo) só dispara uma vez.
    ///
    /// Templates necessários (pré-aprovados na Meta):
    ///   - lembrete_24h(nome, servico, data, hora, local)
    ///   - lembrete_2h(nome, servico, hora)
    /// </summary>
    public class LembreteBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<LembreteBackgroundService> _logger;
        private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromMinutes(5);
        // Janelas: aceita lembrete dentro de [alvo-Δ, alvo+Δ] para tolerar atraso do scheduler
        private static readonly TimeSpan Janela = TimeSpan.FromMinutes(10);

        public LembreteBackgroundService(IServiceProvider services,
            ILogger<LembreteBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LembreteBackgroundService iniciado (intervalo {Intervalo}).", IntervaloVerificacao);
            // Aguarda 1 min antes da primeira execução para não competir com startup
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessarLembretesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no ciclo de lembretes. Continuando.");
                }
                try { await Task.Delay(IntervaloVerificacao, stoppingToken); }
                catch (TaskCanceledException) { return; }
            }
        }

        private async Task ProcessarLembretesAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AgendamentoProDbContext>();
            var whatsapp = scope.ServiceProvider.GetRequiredService<INotificadorWhatsApp>();

            if (!whatsapp.Ativo)
            {
                // Nada a fazer se WhatsApp não está configurado
                return;
            }

            var agora = DateTime.UtcNow;
            await Disparar(ctx, whatsapp, "Lembrete24h", agora.AddHours(24), ct);
            await Disparar(ctx, whatsapp, "Lembrete2h", agora.AddHours(2), ct);
        }

        private async Task Disparar(AgendamentoProDbContext ctx, INotificadorWhatsApp whatsapp,
            string tipo, DateTime alvo, CancellationToken ct)
        {
            var inicio = alvo.AddMinutes(-Janela.TotalMinutes);
            var fim = alvo.AddMinutes(Janela.TotalMinutes);
            var dataAlvo = alvo.Date;

            // Busca agendamentos confirmados na janela alvo, sem notificação prévia desse tipo.
            // Filtro grosso por data; refina por data+hora em memória já que TimeSpan não traduz.
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

                // Idempotência: já notificou esse tipo para esse agendamento?
                var jaEnviou = await ctx.Notificacoes.AnyAsync(n =>
                    n.R_AgeId == ag.AgeId
                    && n.NotTipo == tipo
                    && n.NotStatus == "Enviada", ct);
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
                    else // Lembrete2h
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

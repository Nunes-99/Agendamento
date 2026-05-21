using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Infrastructure.Services.Manutencao
{
    /// <summary>
    /// Apaga registros antigos da tabela LogAuditoria. Default: > 12 meses.
    ///
    /// LGPD: dados de log também devem ter retention; manter pra sempre não é compatível
    /// com o princípio de minimização. Acima de 12 meses, troubleshooting raramente
    /// se beneficia do registro original.
    ///
    /// Em modo Shared (default): roda uma vez no banco compartilhado.
    /// Em modo PerTenant: itera tenants ativos (lidos do DB shared) e purga cada
    /// banco de tenant. Sem isso, os logs dos tenants nunca eram removidos.
    ///
    /// Roda 1x ao dia via Hangfire ("0 4 * * *" - 4h UTC).
    /// </summary>
    public class AuditPurgeJob
    {
        private const int RetencaoMeses = 12;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantConnectionFactory _connFactory;
        private readonly ILogger<AuditPurgeJob> _logger;

        public AuditPurgeJob(IServiceScopeFactory scopeFactory,
            ITenantConnectionFactory connFactory, ILogger<AuditPurgeJob> logger)
        {
            _scopeFactory = scopeFactory;
            _connFactory = connFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 1)]
        public async Task ExecutarAsync(CancellationToken ct)
        {
            if (!_connFactory.IsPerTenant)
            {
                await PurgarParaTenantAsync(tenantId: null, ct);
                return;
            }

            // Lista tenants do DB shared (continua sendo fonte de verdade pra registro de tenants)
            List<int> tenantsAtivos;
            using (var scopeShared = _scopeFactory.CreateScope())
            {
                var tenants = scopeShared.ServiceProvider.GetRequiredService<ITenantRepository>();
                tenantsAtivos = (await tenants.GetAllAsync()).Where(t => t.TenAtivo).Select(t => t.TenId).ToList();
            }

            foreach (var tid in tenantsAtivos)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await PurgarParaTenantAsync(tid, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AuditPurge do tenant {Id} falhou, continuando.", tid);
                }
            }
        }

        private async Task PurgarParaTenantAsync(int? tenantId, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            if (tenantId.HasValue)
            {
                var tCtx = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                tCtx.SetTenant(tenantId.Value, slug: null);
            }

            var ctx = scope.ServiceProvider.GetRequiredService<AgendamentoProDbContext>();
            var corte = DateTime.UtcNow.AddMonths(-RetencaoMeses);
            var deletados = await ctx.LogsAuditoria
                .Where(l => l.LogQuandoUtc < corte)
                .ExecuteDeleteAsync(ct);
            if (deletados > 0)
                _logger.LogInformation("AuditPurgeJob (tenant {Tid}): {N} registros antigos (> {Meses}m) removidos.",
                    tenantId, deletados, RetencaoMeses);
        }
    }

    /// <summary>
    /// Roda script de backup do SQLite + uploads via Hangfire diário (3h UTC).
    /// Em PerTenant não faz sentido — neste modo, backup é por tenant DB.
    /// </summary>
    public class BackupJob
    {
        private readonly ILogger<BackupJob> _logger;
        public BackupJob(ILogger<BackupJob> logger) { _logger = logger; }

        [AutomaticRetry(Attempts = 1)]
        public Task ExecutarAsync(CancellationToken ct)
        {
            // Best-effort: chama o script `scripts/backup-sqlite.sh` se existir.
            // Em produção real, o cron do host pode ser melhor que o Hangfire pra isso.
            try
            {
                var script = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "scripts", "backup-sqlite.sh");
                script = Path.GetFullPath(script);
                if (!File.Exists(script))
                {
                    _logger.LogDebug("BackupJob: script {Path} não encontrado, ignorando.", script);
                    return Task.CompletedTask;
                }

                var psi = new System.Diagnostics.ProcessStartInfo("bash", script)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var p = System.Diagnostics.Process.Start(psi)!;
                p.WaitForExit(timeout: TimeSpan.FromMinutes(10));
                if (p.ExitCode == 0)
                    _logger.LogInformation("BackupJob OK: {Out}", p.StandardOutput.ReadToEnd());
                else
                    _logger.LogWarning("BackupJob falhou ({Code}): {Err}", p.ExitCode, p.StandardError.ReadToEnd());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BackupJob: erro ao executar script.");
            }
            return Task.CompletedTask;
        }
    }
}

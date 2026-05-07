using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Infrastructure.Services.Manutencao
{
    /// <summary>
    /// Apaga registros antigos da tabela LogAuditoria. Default: > 12 meses.
    /// LGPD: dados de log também devem ter retention; manter pra sempre não é compatível
    /// com o princípio de minimização. Acima de 12 meses, troubleshooting raramente
    /// se beneficia do registro original.
    /// Roda 1x ao dia via Hangfire ("0 4 * * *" - 4h UTC).
    /// </summary>
    public class AuditPurgeJob
    {
        private static readonly int RetencaoMeses = 12;

        private readonly AgendamentoProDbContext _ctx;
        private readonly ILogger<AuditPurgeJob> _logger;

        public AuditPurgeJob(AgendamentoProDbContext ctx, ILogger<AuditPurgeJob> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 1)]
        public async Task ExecutarAsync(CancellationToken ct)
        {
            var corte = DateTime.UtcNow.AddMonths(-RetencaoMeses);
            var deletados = await _ctx.LogsAuditoria
                .Where(l => l.LogQuandoUtc < corte)
                .ExecuteDeleteAsync(ct);
            if (deletados > 0)
                _logger.LogInformation("AuditPurgeJob: {N} registros antigos (> {Meses}m) removidos.",
                    deletados, RetencaoMeses);
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

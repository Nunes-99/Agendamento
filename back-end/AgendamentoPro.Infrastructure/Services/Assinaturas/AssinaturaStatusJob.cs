using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Infrastructure.Services.Assinaturas
{
    /// <summary>
    /// Job Hangfire diário que aplica as transições de grace period:
    /// - Ativa com vencimento &gt; 1d atrás e sem pagamento recente → Atrasada (backstop caso webhook não tenha chegado).
    /// - Atrasada há &gt;= 8 dias → ReadOnly.
    /// - ReadOnly há &gt;= 22 dias (= D+30 total) → Expirada + soft delete do tenant.
    ///
    /// Executa todo dia às 03:00 UTC.
    /// </summary>
    public class AssinaturaStatusJob
    {
        private const int DiasParaReadOnly = 8;
        private const int DiasReadOnlyParaExpiracao = 22;
        private const int DiasToleranciaPosVencimento = 1;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AssinaturaStatusJob> _logger;

        public AssinaturaStatusJob(IServiceScopeFactory scopeFactory, ILogger<AssinaturaStatusJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 1800 })]
        public async Task ExecutarAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var assinaturas = scope.ServiceProvider.GetRequiredService<IAssinaturaRepository>();
            var tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var cache = scope.ServiceProvider.GetRequiredService<IAssinaturaCacheInvalidator>();

            var lista = (await assinaturas.ListarAtivasOuInadimplentesAsync()).ToList();
            var agora = DateTime.UtcNow;
            int marcadasAtrasadas = 0, transicionadasReadOnly = 0, expiradas = 0;

            foreach (var ass in lista)
            {
                ct.ThrowIfCancellationRequested();

                // 1. Ativa com vencimento ultrapassado e sem pagamento recente → Atrasada (backstop)
                // Backstop do TRIAL: o normal é o webhook do Mercado Pago virar a
                // assinatura em Ativa (primeira cobrança) ou Atrasada (cobrança recusada)
                // no fim do mês grátis. Se esse aviso se perder, sem esta rede a oficina
                // ficaria em Trial — acesso total, sem nunca pagar. Passado o prazo com
                // tolerância, marca Atrasada e o grace period assume daqui.
                if (ass.AssStatus == StatusAssinatura.Trial
                    && ass.AssTrialAteEm.HasValue
                    && ass.AssTrialAteEm.Value.AddDays(DiasToleranciaPosVencimento) < agora)
                {
                    if (ass.MarcarAtrasada(agora))
                    {
                        await assinaturas.UpdateAsync(ass);
                        cache.Invalidar(ass.R_TenId);
                        marcadasAtrasadas++;
                        _logger.LogWarning("Assinatura {Ass} (tenant {Tid}) saiu do trial sem cobrança confirmada; marcada Atrasada (backstop).",
                            ass.AssId, ass.R_TenId);
                    }
                    continue;
                }

                if (ass.AssStatus == StatusAssinatura.Ativa
                    && ass.AssProximoVencimento.HasValue
                    && ass.AssProximoVencimento.Value.AddDays(DiasToleranciaPosVencimento) < agora)
                {
                    if (ass.MarcarAtrasada(agora))
                    {
                        await assinaturas.UpdateAsync(ass);
                        cache.Invalidar(ass.R_TenId);
                        marcadasAtrasadas++;
                        _logger.LogWarning("Assinatura {Ass} (tenant {Tid}) marcada como Atrasada (backstop por vencimento).",
                            ass.AssId, ass.R_TenId);
                    }
                    continue;
                }

                // 2. Atrasada há >= 8 dias → ReadOnly
                if (ass.AssStatus == StatusAssinatura.Atrasada
                    && ass.AssAtrasoDesde.HasValue
                    && ass.AssAtrasoDesde.Value.AddDays(DiasParaReadOnly) <= agora)
                {
                    if (ass.TransicionarReadOnly(agora))
                    {
                        await assinaturas.UpdateAsync(ass);
                        cache.Invalidar(ass.R_TenId);
                        transicionadasReadOnly++;
                        _logger.LogWarning("Assinatura {Ass} (tenant {Tid}) movida para ReadOnly.",
                            ass.AssId, ass.R_TenId);
                    }
                    continue;
                }

                // 3. ReadOnly há >= 22 dias (D+30 total) → Expirada + soft delete tenant
                if (ass.AssStatus == StatusAssinatura.ReadOnly
                    && ass.AssReadOnlyDesde.HasValue
                    && ass.AssReadOnlyDesde.Value.AddDays(DiasReadOnlyParaExpiracao) <= agora)
                {
                    if (ass.Expirar(agora))
                    {
                        await assinaturas.UpdateAsync(ass);
                        cache.Invalidar(ass.R_TenId);

                        // Soft delete + inativa o tenant. Dados preservados 90d para reativação.
                        var tenant = await tenants.GetByIdAsync(ass.R_TenId);
                        if (tenant != null)
                        {
                            tenant.Inativar();
                            tenant.Excluir();
                            await tenants.UpdateAsync(tenant);
                        }

                        expiradas++;
                        _logger.LogError("Assinatura {Ass} (tenant {Tid}) EXPIRADA — tenant soft deleted.",
                            ass.AssId, ass.R_TenId);
                    }
                }
            }

            if (marcadasAtrasadas + transicionadasReadOnly + expiradas > 0)
                await uow.SaveChangesAsync();

            _logger.LogInformation("AssinaturaStatusJob: {Atr} atrasadas, {Ro} read-only, {Exp} expiradas.",
                marcadasAtrasadas, transicionadasReadOnly, expiradas);
        }
    }
}

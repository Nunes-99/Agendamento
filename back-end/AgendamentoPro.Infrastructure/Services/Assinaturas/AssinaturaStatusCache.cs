using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace AgendamentoPro.Infrastructure.Services.Assinaturas
{
    /// <summary>
    /// Cache em memória do status da assinatura por tenant. Reduz pressão de DB no
    /// AssinaturaGuardMiddleware (1 query por request → 1 query por tenant a cada 30s).
    ///
    /// Stale-window: até 30s entre uma mutação não-invalidada e o reflexo no guard.
    /// Use cases que mutam status DEVEM chamar Invalidar(tenantId) — feito via IAssinaturaCacheInvalidator.
    /// </summary>
    public class AssinaturaStatusCache : IAssinaturaCacheInvalidator
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

        private readonly IMemoryCache _cache;

        public AssinaturaStatusCache(IMemoryCache cache) { _cache = cache; }

        /// <summary>Retorna status do cache; cache-miss busca no repo e cacheia.</summary>
        public async Task<StatusAssinatura?> ObterStatusAsync(int tenantId, IAssinaturaRepository repo)
        {
            var key = Chave(tenantId);
            if (_cache.TryGetValue<StatusBox>(key, out var box))
                return box.Status;

            // GetUltimaByTenantAsync (sem filtro de status): o guard precisa enxergar
            // Cancelada/Expirada — GetByTenantAsync as esconde e liberaria o tenant.
            var ass = await repo.GetUltimaByTenantAsync(tenantId);
            var status = ass?.AssStatus;
            _cache.Set(key, new StatusBox(status), Ttl);
            return status;
        }

        public void Invalidar(int tenantId) => _cache.Remove(Chave(tenantId));

        private static string Chave(int tenantId) => $"ass-status:{tenantId}";

        // Boxing explícito porque IMemoryCache.TryGetValue<T?> não distingue
        // "ausente" de "presente com valor null" para Nullable<T>.
        private sealed record StatusBox(StatusAssinatura? Status);
    }
}

using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;

namespace AgendamentoPro.Infrastructure.Services.Cache
{
    /// <summary>
    /// Implementação de ITenantCache em cima de IMemoryCache. As chaves são
    /// prefixadas automaticamente com `tenant:{id}:` quando há tenant resolvido,
    /// evitando bleed-through entre tenants num único processo.
    ///
    /// Quando não há tenant resolvido, cai num namespace `_global:` (também isolado
    /// dos tenants — evita poluir cache geral com chaves órfãs).
    /// </summary>
    public class TenantAwareMemoryCache : ITenantCache
    {
        private readonly IMemoryCache _cache;
        private readonly ITenantContext _tenant;

        public TenantAwareMemoryCache(IMemoryCache cache, ITenantContext tenant)
        {
            _cache = cache;
            _tenant = tenant;
        }

        private string PrefixoTenant() =>
            _tenant.IsResolved && _tenant.TenantId.HasValue
                ? $"tenant:{_tenant.TenantId.Value}:"
                : "_orphan:";

        private string ChaveCompleta(string chave) => PrefixoTenant() + chave;

        public T Get<T>(string chave) where T : class =>
            _cache.TryGetValue(ChaveCompleta(chave), out T v) ? v : null;

        public void Set<T>(string chave, T valor, TimeSpan ttl) where T : class =>
            _cache.Set(ChaveCompleta(chave), valor, ttl);

        public void Remove(string chave) =>
            _cache.Remove(ChaveCompleta(chave));

        public async Task<T> GetOrCreateAsync<T>(string chave, TimeSpan ttl, Func<Task<T>> factory) where T : class
        {
            var k = ChaveCompleta(chave);
            if (_cache.TryGetValue(k, out T cached)) return cached;
            var v = await factory();
            if (v != null) _cache.Set(k, v, ttl);
            return v;
        }

        public T GetGlobal<T>(string chave) where T : class =>
            _cache.TryGetValue("_global:" + chave, out T v) ? v : null;

        public void SetGlobal<T>(string chave, T valor, TimeSpan ttl) where T : class =>
            _cache.Set("_global:" + chave, valor, ttl);
    }
}

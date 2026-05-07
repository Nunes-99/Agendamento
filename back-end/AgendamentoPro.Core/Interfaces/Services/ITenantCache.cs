namespace AgendamentoPro.Core.Interfaces.Services
{
    /// <summary>
    /// Cache em memória com isolamento automático por tenant.
    /// Chaves recebem prefixo `tenant:{id}:` antes de bater no IMemoryCache,
    /// evitando vazamento entre tenants em ambiente single-process.
    ///
    /// Para cache compartilhado entre tenants (ex: catálogo público sem login),
    /// use a sobrecarga global ou o IMemoryCache diretamente.
    /// </summary>
    public interface ITenantCache
    {
        /// <summary>Obtém valor; null se não presente. Usa contexto de tenant atual.</summary>
        T Get<T>(string chave) where T : class;

        /// <summary>Armazena valor com TTL no contexto de tenant atual.</summary>
        void Set<T>(string chave, T valor, TimeSpan ttl) where T : class;

        /// <summary>Remove valor do contexto de tenant atual.</summary>
        void Remove(string chave);

        /// <summary>Get-or-create: se não existir, executa o factory, cacheia e retorna.</summary>
        Task<T> GetOrCreateAsync<T>(string chave, TimeSpan ttl, Func<Task<T>> factory) where T : class;

        /// <summary>Get global (sem prefixo de tenant), pra dados não isolados.</summary>
        T GetGlobal<T>(string chave) where T : class;
        void SetGlobal<T>(string chave, T valor, TimeSpan ttl) where T : class;
    }
}

using AgendamentoPro.Core.Entities.Tenants;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IConfiguracaoTenantRepository
    {
        Task<IEnumerable<ConfiguracaoTenant>> GetByTenantAsync(int tenantId);
        Task<ConfiguracaoTenant> GetByChaveAsync(int tenantId, string chave);
        Task<int> CreateAsync(ConfiguracaoTenant config);
        Task UpdateAsync(ConfiguracaoTenant config);
        Task DeleteAsync(int id);
    }
}

using AgendamentoPro.Core.Entities.Tenants;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface ITenantRepository
    {
        Task<Tenant> GetByIdAsync(int id);
        Task<Tenant> GetBySlugAsync(string slug);
        Task<IEnumerable<Tenant>> GetAllAsync();
        Task<int> CreateAsync(Tenant tenant);
        Task UpdateAsync(Tenant tenant);
        Task<bool> SlugDisponivelAsync(string slug, int? ignorarId = null);
    }
}

using AgendamentoPro.Core.Entities.Servicos;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IComboRepository
    {
        Task<Combo> GetByIdAsync(int id, int tenantId);
        Task<IEnumerable<Combo>> GetByTenantAsync(int tenantId, bool somenteAtivos);
        Task<int> CreateAsync(Combo combo);
        Task UpdateAsync(Combo combo);
        Task DeleteAsync(int id, int tenantId);
    }
}

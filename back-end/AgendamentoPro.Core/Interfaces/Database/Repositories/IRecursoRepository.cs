using AgendamentoPro.Core.Entities.Recursos;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IRecursoRepository
    {
        Task<Recurso> GetByIdAsync(int id, int tenantId);
        Task<IEnumerable<Recurso>> GetByTenantAsync(int tenantId, bool somenteAtivos);
        Task<int> CreateAsync(Recurso recurso);
        Task UpdateAsync(Recurso recurso);
        Task DeleteAsync(int id, int tenantId);
    }
}

using AgendamentoPro.Core.Entities.Servicos;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IServicoRepository
    {
        Task<Servico> GetByIdAsync(int id, int tenantId);
        Task<IEnumerable<Servico>> GetByTenantAsync(int tenantId, bool somenteAtivos);
        Task<int> CreateAsync(Servico servico);
        Task UpdateAsync(Servico servico);
        Task DeleteAsync(int id, int tenantId);
    }
}

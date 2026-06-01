using AgendamentoPro.Core.Entities.Assinaturas;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IPlanoRepository
    {
        Task<Plano> GetByIdAsync(int id);
        Task<IEnumerable<Plano>> ListarPublicosAsync();
        Task<IEnumerable<Plano>> ListarTodosAsync();
        Task<int> CreateAsync(Plano plano);
        Task UpdateAsync(Plano plano);
    }
}

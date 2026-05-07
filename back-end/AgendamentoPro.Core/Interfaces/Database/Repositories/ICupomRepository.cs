using AgendamentoPro.Core.Entities.Servicos;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface ICupomRepository
    {
        Task<Cupom> GetByCodigoAsync(int tenantId, string codigo);
        Task UpdateAsync(Cupom cupom);
    }
}

using AgendamentoPro.Core.Entities.Assinaturas;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IAssinaturaRepository
    {
        Task<Assinatura> GetByIdAsync(int id);
        Task<Assinatura> GetByTenantAsync(int tenantId);
        Task<Assinatura> GetByGatewayPreapprovalIdAsync(string preapprovalId);
        /// <summary>Lista assinaturas que podem precisar de transição de status (não Cancelada/Expirada).</summary>
        Task<IEnumerable<Assinatura>> ListarAtivasOuInadimplentesAsync();
        Task<int> CreateAsync(Assinatura assinatura);
        Task UpdateAsync(Assinatura assinatura);
    }
}

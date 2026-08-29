using AgendamentoPro.Core.Entities.Assinaturas;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IAssinaturaRepository
    {
        Task<Assinatura> GetByIdAsync(int id);
        Task<Assinatura> GetByTenantAsync(int tenantId);
        /// <summary>
        /// Última assinatura do tenant SEM filtrar status — inclui Cancelada/Expirada.
        /// É a fonte do guard de acesso: GetByTenantAsync esconde Cancelada/Expirada
        /// (para permitir re-assinar), o que fazia tenants cancelados parecerem
        /// "sem assinatura" e passarem livres pelo bloqueio.
        /// </summary>
        Task<Assinatura> GetUltimaByTenantAsync(int tenantId);
        Task<Assinatura> GetByGatewayPreapprovalIdAsync(string preapprovalId);
        /// <summary>Lista assinaturas que podem precisar de transição de status (não Cancelada/Expirada).</summary>
        Task<IEnumerable<Assinatura>> ListarAtivasOuInadimplentesAsync();
        Task<int> CreateAsync(Assinatura assinatura);
        Task UpdateAsync(Assinatura assinatura);
        /// <summary>Remove uma assinatura que nunca chegou ao gateway (rascunho órfão).</summary>
        Task DeleteAsync(Assinatura assinatura);
    }
}

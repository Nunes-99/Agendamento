using AgendamentoPro.Core.Entities.Clientes;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IPontosFidelidadeRepository
    {
        /// <summary>Pega o registro de pontos do cliente, ou null.</summary>
        Task<PontosFidelidade> GetAsync(int tenantId, int clienteId);
        Task<int> CreateAsync(PontosFidelidade pontos);
        Task UpdateAsync(PontosFidelidade pontos);
    }
}

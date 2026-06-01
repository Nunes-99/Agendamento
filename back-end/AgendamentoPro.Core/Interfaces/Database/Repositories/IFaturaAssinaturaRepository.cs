using AgendamentoPro.Core.Entities.Assinaturas;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IFaturaAssinaturaRepository
    {
        Task<FaturaAssinatura> GetByIdAsync(int id);
        Task<FaturaAssinatura> GetByGatewayPaymentIdAsync(string gatewayPaymentId);
        Task<IEnumerable<FaturaAssinatura>> ListarPorAssinaturaAsync(int assinaturaId);
        Task<int> CreateAsync(FaturaAssinatura fatura);
        Task UpdateAsync(FaturaAssinatura fatura);
    }
}

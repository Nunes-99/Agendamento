using AgendamentoPro.Core.Entities.Pagamentos;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IPagamentoRepository
    {
        Task<Pagamento> GetByIdAsync(int id);
        Task<Pagamento> GetByGatewayIdAsync(string gatewayId);
        Task<IEnumerable<Pagamento>> GetByAgendamentoAsync(int agendamentoId);
        Task<int> CreateAsync(Pagamento pagamento);
        Task UpdateAsync(Pagamento pagamento);
    }
}

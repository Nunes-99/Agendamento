using AgendamentoPro.Core.Entities.Servicos;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface ISaldoPacoteRepository
    {
        /// <summary>
        /// Saldo válido (com quantidade restante e não expirado) do cliente
        /// para o serviço dado. Retorna null se não tiver pacote ativo.
        /// </summary>
        Task<SaldoPacote> GetSaldoValidoAsync(int tenantId, int clienteId, int servicoId);

        /// <summary>Saldo pendente vinculado a um gatewayId — usado pelo webhook MP.</summary>
        Task<SaldoPacote> GetByGatewayIdAsync(string gatewayId);

        Task<int> CreateAsync(SaldoPacote saldo);
        Task UpdateAsync(SaldoPacote saldo);
    }
}

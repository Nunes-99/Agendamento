using AgendamentoPro.Core.Entities.Clientes;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IClienteRepository
    {
        Task<Cliente> GetByIdAsync(int id, int tenantId);
        Task<Cliente> GetByTelefoneAsync(int tenantId, string telefone);
        Task<Cliente> GetByEmailAsync(int tenantId, string email);
        Task<(IEnumerable<Cliente> Items, int Total)> GetPagedAsync(int tenantId, int page, int pageSize, string busca);
        Task<int> CreateAsync(Cliente cliente);
        Task UpdateAsync(Cliente cliente);
        Task DeleteAsync(int id, int tenantId);

        /// <summary>
        /// Retorna IDs de clientes do tenant cujo último agendamento (não cancelado)
        /// é mais antigo que o limite OU que nunca tiveram agendamento E foram criados
        /// antes do limite. Usado pela LGPD para anonimização em massa.
        /// Single query agregada — evita N+1.
        /// </summary>
        Task<IEnumerable<int>> GetIdsInativosAsync(int tenantId, DateTime corte);
    }
}

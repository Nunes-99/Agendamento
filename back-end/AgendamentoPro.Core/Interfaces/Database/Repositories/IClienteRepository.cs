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
    }
}

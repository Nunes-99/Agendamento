using AgendamentoPro.Core.Entities.Usuarios;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IUsuarioRepository
    {
        Task<Usuario> GetByIdAsync(int id);
        Task<Usuario> GetByEmailAsync(string email);
        Task<IEnumerable<Usuario>> GetByTenantAsync(int tenantId);
        Task<int> CreateAsync(Usuario usuario);
        Task UpdateAsync(Usuario usuario);
    }
}

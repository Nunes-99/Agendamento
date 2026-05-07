using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public UsuarioRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Usuario> GetByIdAsync(int id)
            => _ctx.Usuarios.FirstOrDefaultAsync(u => u.UsuId == id && !u.Excluido);

        public Task<Usuario> GetByEmailAsync(string email)
            => _ctx.Usuarios.FirstOrDefaultAsync(u => u.UsuEmail == email && !u.Excluido);

        public async Task<IEnumerable<Usuario>> GetByTenantAsync(int tenantId)
            => await _ctx.Usuarios.AsNoTracking().Where(u => u.R_TenId == tenantId && !u.Excluido).ToListAsync();

        public async Task<int> CreateAsync(Usuario usuario)
        {
            _ctx.Usuarios.Add(usuario);
            await _ctx.SaveChangesAsync();
            return usuario.UsuId;
        }

        public Task UpdateAsync(Usuario usuario)
        {
            _ctx.Usuarios.Update(usuario);
            return Task.CompletedTask;
        }
    }
}

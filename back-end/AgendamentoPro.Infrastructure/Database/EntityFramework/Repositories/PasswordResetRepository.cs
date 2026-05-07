using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public PasswordResetRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<PasswordReset> GetByTokenAsync(string token)
            => _ctx.PasswordResets.FirstOrDefaultAsync(r => r.RpsToken == token);

        public async Task<int> CreateAsync(PasswordReset reset)
        {
            _ctx.PasswordResets.Add(reset);
            await _ctx.SaveChangesAsync();
            return reset.RpsId;
        }

        public Task UpdateAsync(PasswordReset reset)
        {
            _ctx.PasswordResets.Update(reset);
            return Task.CompletedTask;
        }

        public async Task InvalidarPendentesAsync(int usuarioId)
        {
            var pendentes = await _ctx.PasswordResets
                .Where(r => r.R_UsuId == usuarioId && !r.RpsUsado)
                .ToListAsync();
            foreach (var p in pendentes) p.MarcarUsado();
        }
    }
}

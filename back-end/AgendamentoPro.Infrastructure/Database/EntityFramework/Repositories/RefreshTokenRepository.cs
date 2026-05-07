using AgendamentoPro.Core.Entities.RefreshTokens;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public RefreshTokenRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<RefreshToken> GetByTokenAsync(string token)
            => _ctx.RefreshTokens.FirstOrDefaultAsync(r => r.RefToken == token);

        public async Task<int> CreateAsync(RefreshToken token)
        {
            _ctx.RefreshTokens.Add(token);
            await _ctx.SaveChangesAsync();
            return token.RefId;
        }

        public Task UpdateAsync(RefreshToken token)
        {
            _ctx.RefreshTokens.Update(token);
            return Task.CompletedTask;
        }

        public async Task RevogarTodosDoUsuarioAsync(int usuarioId)
        {
            var tokens = await _ctx.RefreshTokens.Where(r => r.R_UsuId == usuarioId).ToListAsync();
            foreach (var t in tokens) t.Revogar();
        }
    }
}

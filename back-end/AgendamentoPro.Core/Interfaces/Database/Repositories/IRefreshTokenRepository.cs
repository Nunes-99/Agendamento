using AgendamentoPro.Core.Entities.RefreshTokens;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> GetByTokenAsync(string token);
        Task<int> CreateAsync(RefreshToken token);
        Task UpdateAsync(RefreshToken token);
        Task RevogarTodosDoUsuarioAsync(int usuarioId);
    }
}

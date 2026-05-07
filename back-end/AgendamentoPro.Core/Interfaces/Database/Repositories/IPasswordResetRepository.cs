using AgendamentoPro.Core.Entities.Usuarios;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IPasswordResetRepository
    {
        Task<PasswordReset> GetByTokenAsync(string token);
        Task<int> CreateAsync(PasswordReset reset);
        Task UpdateAsync(PasswordReset reset);
        Task InvalidarPendentesAsync(int usuarioId);
    }
}

using AgendamentoPro.Core.Entities.Usuarios;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IOtpChallengeRepository
    {
        Task<OtpChallenge> GetUltimoAtivoAsync(int tenantId, string telefone);
        Task<int> ContarRecentesAsync(int tenantId, string telefone, DateTime desde);
        Task CreateAsync(OtpChallenge challenge);
        Task UpdateAsync(OtpChallenge challenge);
    }
}

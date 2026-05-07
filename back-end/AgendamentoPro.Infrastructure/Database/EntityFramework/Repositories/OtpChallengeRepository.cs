using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class OtpChallengeRepository : IOtpChallengeRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public OtpChallengeRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<OtpChallenge> GetUltimoAtivoAsync(int tenantId, string telefone)
            => _ctx.OtpChallenges
                .Where(o => o.R_TenId == tenantId && o.OtpTelefone == telefone
                    && !o.OtpUsado && o.OtpExpiraEm > DateTime.UtcNow)
                .OrderByDescending(o => o.OtpCriadoEm)
                .FirstOrDefaultAsync();

        public Task<int> ContarRecentesAsync(int tenantId, string telefone, DateTime desde)
            => _ctx.OtpChallenges.CountAsync(o =>
                o.R_TenId == tenantId && o.OtpTelefone == telefone && o.OtpCriadoEm >= desde);

        public async Task CreateAsync(OtpChallenge challenge)
        {
            await _ctx.OtpChallenges.AddAsync(challenge);
        }

        public Task UpdateAsync(OtpChallenge challenge)
        {
            _ctx.OtpChallenges.Update(challenge);
            return Task.CompletedTask;
        }
    }
}

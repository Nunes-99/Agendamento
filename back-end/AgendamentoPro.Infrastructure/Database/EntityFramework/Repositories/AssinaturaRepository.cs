using AgendamentoPro.Core.Entities.Assinaturas;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class AssinaturaRepository : IAssinaturaRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public AssinaturaRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Assinatura> GetByIdAsync(int id)
            => _ctx.Assinaturas.Include(a => a.Plano).FirstOrDefaultAsync(a => a.AssId == id);

        public Task<Assinatura> GetByTenantAsync(int tenantId)
            => _ctx.Assinaturas.Include(a => a.Plano)
                .Where(a => a.R_TenId == tenantId
                            && a.AssStatus != StatusAssinatura.Cancelada
                            && a.AssStatus != StatusAssinatura.Expirada)
                .OrderByDescending(a => a.AssCriadoEm)
                .FirstOrDefaultAsync();

        public Task<Assinatura> GetUltimaByTenantAsync(int tenantId)
            => _ctx.Assinaturas.Include(a => a.Plano)
                .Where(a => a.R_TenId == tenantId)
                .OrderByDescending(a => a.AssCriadoEm)
                .FirstOrDefaultAsync();

        public Task<Assinatura> GetByGatewayPreapprovalIdAsync(string preapprovalId)
            => _ctx.Assinaturas.FirstOrDefaultAsync(a => a.AssGatewayPreapprovalId == preapprovalId);

        public async Task<IEnumerable<Assinatura>> ListarAtivasOuInadimplentesAsync()
            => await _ctx.Assinaturas.AsNoTracking().Include(a => a.Plano)
                .Where(a => a.AssStatus == StatusAssinatura.Trial
                         || a.AssStatus == StatusAssinatura.Ativa
                         || a.AssStatus == StatusAssinatura.Atrasada
                         || a.AssStatus == StatusAssinatura.ReadOnly)
                .ToListAsync();

        public async Task<int> CreateAsync(Assinatura assinatura)
        {
            _ctx.Assinaturas.Add(assinatura);
            await _ctx.SaveChangesAsync();
            return assinatura.AssId;
        }

        public Task UpdateAsync(Assinatura assinatura)
        {
            _ctx.Assinaturas.Update(assinatura);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Assinatura assinatura)
        {
            _ctx.Assinaturas.Remove(assinatura);
            await _ctx.SaveChangesAsync();
        }
    }
}

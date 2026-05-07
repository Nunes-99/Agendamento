using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class ConfiguracaoTenantRepository : IConfiguracaoTenantRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public ConfiguracaoTenantRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public async Task<IEnumerable<ConfiguracaoTenant>> GetByTenantAsync(int tenantId)
            => await _ctx.ConfiguracoesTenant.AsNoTracking().Where(c => c.R_TenId == tenantId).ToListAsync();

        public Task<ConfiguracaoTenant> GetByChaveAsync(int tenantId, string chave)
            => _ctx.ConfiguracoesTenant.FirstOrDefaultAsync(c => c.R_TenId == tenantId && c.CfgChave == chave);

        public async Task<int> CreateAsync(ConfiguracaoTenant config)
        {
            _ctx.ConfiguracoesTenant.Add(config);
            await _ctx.SaveChangesAsync();
            return config.CfgId;
        }

        public Task UpdateAsync(ConfiguracaoTenant config)
        {
            _ctx.ConfiguracoesTenant.Update(config);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var c = await _ctx.ConfiguracoesTenant.FindAsync(id);
            if (c != null) _ctx.ConfiguracoesTenant.Remove(c);
        }
    }
}

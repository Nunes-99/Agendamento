using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class TenantRepository : ITenantRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public TenantRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Tenant> GetByIdAsync(int id)
            => _ctx.Tenants.FirstOrDefaultAsync(t => t.TenId == id && !t.Excluido);

        public Task<Tenant> GetBySlugAsync(string slug)
            => _ctx.Tenants.FirstOrDefaultAsync(t => t.TenSlug == slug && !t.Excluido);

        public async Task<IEnumerable<Tenant>> GetAllAsync()
            => await _ctx.Tenants.AsNoTracking().Where(t => !t.Excluido).OrderBy(t => t.TenNome).ToListAsync();

        public async Task<int> CreateAsync(Tenant tenant)
        {
            _ctx.Tenants.Add(tenant);
            await _ctx.SaveChangesAsync();
            return tenant.TenId;
        }

        public Task UpdateAsync(Tenant tenant)
        {
            _ctx.Tenants.Update(tenant);
            return Task.CompletedTask;
        }

        public async Task<bool> SlugDisponivelAsync(string slug, int? ignorarId = null)
        {
            slug = (slug ?? string.Empty).ToLowerInvariant();
            return !await _ctx.Tenants.AnyAsync(t => t.TenSlug == slug
                && (!ignorarId.HasValue || t.TenId != ignorarId.Value));
        }
    }
}

using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class WebPushSubscriptionRepository : IWebPushSubscriptionRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public WebPushSubscriptionRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<WebPushSubscription> GetByEndpointAsync(string endpoint)
            => _ctx.WebPushSubscriptions.FirstOrDefaultAsync(x => x.PushEndpoint == endpoint);

        public async Task<IEnumerable<WebPushSubscription>> GetByTenantAsync(int tenantId)
            => await _ctx.WebPushSubscriptions.AsNoTracking()
                .Where(x => x.R_TenId == tenantId).ToListAsync();

        public async Task<IEnumerable<WebPushSubscription>> GetByUsuarioAsync(int tenantId, int usuarioId)
            => await _ctx.WebPushSubscriptions.AsNoTracking()
                .Where(x => x.R_TenId == tenantId && x.R_UsuId == usuarioId).ToListAsync();

        public async Task<int> CreateAsync(WebPushSubscription sub)
        {
            _ctx.WebPushSubscriptions.Add(sub);
            await _ctx.SaveChangesAsync();
            return sub.PushId;
        }

        public async Task DeleteAsync(int id)
        {
            var found = await _ctx.WebPushSubscriptions.FirstOrDefaultAsync(x => x.PushId == id);
            if (found != null) _ctx.WebPushSubscriptions.Remove(found);
            await _ctx.SaveChangesAsync();
        }

        public async Task DeleteByEndpointAsync(string endpoint)
        {
            var found = await _ctx.WebPushSubscriptions.FirstOrDefaultAsync(x => x.PushEndpoint == endpoint);
            if (found != null) _ctx.WebPushSubscriptions.Remove(found);
            await _ctx.SaveChangesAsync();
        }

        public Task UpdateAsync(WebPushSubscription sub)
        {
            _ctx.WebPushSubscriptions.Update(sub);
            return Task.CompletedTask;
        }
    }
}

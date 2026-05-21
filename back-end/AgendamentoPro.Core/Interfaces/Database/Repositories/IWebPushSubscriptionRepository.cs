using AgendamentoPro.Core.Entities.Usuarios;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IWebPushSubscriptionRepository
    {
        Task<WebPushSubscription> GetByEndpointAsync(string endpoint);
        Task<IEnumerable<WebPushSubscription>> GetByTenantAsync(int tenantId);
        Task<IEnumerable<WebPushSubscription>> GetByUsuarioAsync(int tenantId, int usuarioId);
        Task<int> CreateAsync(WebPushSubscription sub);
        Task DeleteAsync(int id);
        Task DeleteByEndpointAsync(string endpoint);
        Task UpdateAsync(WebPushSubscription sub);
    }
}

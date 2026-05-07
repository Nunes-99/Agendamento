using AgendamentoPro.Core.Entities.Pagamentos;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IWebhookEventoRepository
    {
        Task<WebhookEvento> GetAsync(string gateway, string eventoId);
        Task<int> CreateAsync(WebhookEvento evento);
        Task UpdateAsync(WebhookEvento evento);
    }
}

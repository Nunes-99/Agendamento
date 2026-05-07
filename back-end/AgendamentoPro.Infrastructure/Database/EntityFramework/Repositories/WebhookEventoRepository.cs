using AgendamentoPro.Core.Entities.Pagamentos;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class WebhookEventoRepository : IWebhookEventoRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public WebhookEventoRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<WebhookEvento> GetAsync(string gateway, string eventoId)
            => _ctx.WebhookEventos.AsNoTracking()
                .FirstOrDefaultAsync(w => w.WhEvGateway == gateway && w.WhEvEventoId == eventoId);

        public async Task<int> CreateAsync(WebhookEvento evento)
        {
            _ctx.WebhookEventos.Add(evento);
            await _ctx.SaveChangesAsync();
            return evento.WhEvId;
        }

        public Task UpdateAsync(WebhookEvento evento)
        {
            _ctx.WebhookEventos.Update(evento);
            return Task.CompletedTask;
        }
    }
}

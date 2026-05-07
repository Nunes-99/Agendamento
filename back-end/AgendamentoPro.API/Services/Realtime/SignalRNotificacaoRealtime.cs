using AgendamentoPro.API.Hubs;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace AgendamentoPro.API.Services.Realtime
{
    /// <summary>
    /// Implementação SignalR do INotificacaoRealtime.
    /// Depende do NotificacoesHub configurado em Program.cs.
    /// Vive no projeto API (Infrastructure não pode referenciar API).
    /// </summary>
    public class SignalRNotificacaoRealtime : INotificacaoRealtime
    {
        private readonly IHubContext<NotificacoesHub> _hub;

        public SignalRNotificacaoRealtime(IHubContext<NotificacoesHub> hub) { _hub = hub; }

        public Task NotificarTenantAsync(int tenantId, string evento, object payload)
            => _hub.Clients.Group($"tenant-{tenantId}").SendAsync(evento, payload);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AgendamentoPro.API.Hubs
{
    /// <summary>
    /// Hub SignalR para notificações realtime ao admin.
    /// Eventos: novo agendamento, pagamento aprovado, foto enviada, etc.
    /// Cada admin entra no grupo "tenant-{id}" pra receber só do próprio tenant.
    ///
    /// SEGURANÇA: usa Policy "Atendente" — sem isso, JWT de cliente OTP (que
    /// também carrega claim tenantId) seria aceito e o cliente final receberia
    /// todas as notificações administrativas (nome de outros clientes, valores
    /// pagos, motivos de cancelamento).
    /// </summary>
    [Authorize(Policy = "Atendente")]
    public class NotificacoesHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var tenantClaim = Context.User?.FindFirst("tenantId")?.Value;
            if (int.TryParse(tenantClaim, out var tid))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tid}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var tenantClaim = Context.User?.FindFirst("tenantId")?.Value;
            if (int.TryParse(tenantClaim, out var tid))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant-{tid}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}

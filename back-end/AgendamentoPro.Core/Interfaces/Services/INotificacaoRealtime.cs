namespace AgendamentoPro.Core.Interfaces.Services
{
    /// <summary>
    /// Abstração de notificações realtime para o admin (push via SignalR).
    /// Implementação default: SignalRNotificacaoRealtime; pode ser substituída
    /// em testes por mock.
    /// </summary>
    public interface INotificacaoRealtime
    {
        Task NotificarTenantAsync(int tenantId, string evento, object payload);
    }
}

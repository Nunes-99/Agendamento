namespace AgendamentoPro.Core.Interfaces.Services
{
    /// <summary>
    /// Envia uma notificação Web Push. Implementação no Infrastructure
    /// usa a lib `WebPush` (VAPID); se chaves não configuradas, vira no-op.
    /// </summary>
    public interface IWebPushSender
    {
        /// <summary>True se VAPID está configurado e envios funcionarão.</summary>
        bool Ativo { get; }

        /// <summary>Chave pública VAPID (usada pelo frontend pra subscrever).</summary>
        string ChavePublica { get; }

        /// <summary>
        /// Envia notificação a todos os dispositivos do tenant. Falha de envio
        /// (subscription expirada / 410 Gone) deve remover a subscription.
        /// </summary>
        Task NotificarTenantAsync(int tenantId, string titulo, string corpo, string url = null);
    }
}

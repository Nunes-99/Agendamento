using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Usuarios
{
    /// <summary>
    /// Subscription Web Push de um usuário admin/atendente. Permite enviar
    /// notificações ao dispositivo mesmo com a aba fechada (complementa o
    /// SignalR, que só funciona com app aberto).
    ///
    /// As chaves vêm do browser via Service Worker (PushSubscription.toJSON()):
    /// endpoint + keys.p256dh + keys.auth. O VAPID público é fixo do servidor.
    /// </summary>
    public class WebPushSubscription : ITenantScoped
    {
        public int PushId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_UsuId { get; private set; }
        public string PushEndpoint { get; private set; }
        public string PushP256dh { get; private set; }
        public string PushAuth { get; private set; }
        public string PushUserAgent { get; private set; }
        public DateTime PushCriadoEm { get; private set; }
        public DateTime? PushUltimoEnvio { get; private set; }

        public Tenant Tenant { get; private set; }
        public Usuario Usuario { get; private set; }

        protected WebPushSubscription() { }

        public WebPushSubscription(int rTenId, int rUsuId, string endpoint,
            string p256dh, string auth, string userAgent)
        {
            if (rTenId <= 0) throw new DomainException("Tenant é obrigatório.");
            if (rUsuId <= 0) throw new DomainException("Usuário é obrigatório.");
            if (string.IsNullOrWhiteSpace(endpoint)) throw new DomainException("Endpoint é obrigatório.");
            if (string.IsNullOrWhiteSpace(p256dh)) throw new DomainException("Chave p256dh é obrigatória.");
            if (string.IsNullOrWhiteSpace(auth)) throw new DomainException("Chave auth é obrigatória.");

            R_TenId = rTenId;
            R_UsuId = rUsuId;
            PushEndpoint = endpoint;
            PushP256dh = p256dh;
            PushAuth = auth;
            PushUserAgent = userAgent;
            PushCriadoEm = DateTime.UtcNow;
        }

        public void MarcarEnvioRealizado() => PushUltimoEnvio = DateTime.UtcNow;
    }
}

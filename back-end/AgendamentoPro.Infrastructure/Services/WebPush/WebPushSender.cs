using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WebPushLib = WebPush;

namespace AgendamentoPro.Infrastructure.Services.WebPush
{
    /// <summary>
    /// Envia Web Push (VAPID) usando a lib WebPush.
    ///
    /// <para>Configuração (em ordem de prioridade): env vars → IConfiguration.</para>
    /// <list type="bullet">
    /// <item>VAPID_PUBLIC_KEY (chave pública)</item>
    /// <item>VAPID_PRIVATE_KEY (chave privada)</item>
    /// <item>VAPID_SUBJECT (default: mailto:admin@agendamentopro.local)</item>
    /// </list>
    /// <para>Gere o par com `VapidHelper.GenerateVapidKeys()` — feito uma única vez,
    /// nunca rotacionar (browsers guardam a chave pública na subscription).</para>
    ///
    /// Sem VAPID configurado: <see cref="Ativo"/> = false e os envios são no-op.
    /// </summary>
    public class WebPushSender : IWebPushSender
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<WebPushSender> _logger;
        private readonly WebPushLib.VapidDetails _vapid;
        private readonly WebPushLib.WebPushClient _client;

        public WebPushSender(IServiceScopeFactory scopes, IConfiguration config, ILogger<WebPushSender> logger)
        {
            _scopes = scopes;
            _logger = logger;

            var pub = Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY") ?? config["WebPush:PublicKey"];
            var priv = Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY") ?? config["WebPush:PrivateKey"];
            var subject = Environment.GetEnvironmentVariable("VAPID_SUBJECT")
                ?? config["WebPush:Subject"] ?? "mailto:admin@agendamentopro.local";

            if (!string.IsNullOrWhiteSpace(pub) && !string.IsNullOrWhiteSpace(priv))
            {
                _vapid = new WebPushLib.VapidDetails(subject, pub, priv);
                _client = new WebPushLib.WebPushClient();
                ChavePublica = pub;
                Ativo = true;
                _logger.LogInformation("WebPushSender ativo (VAPID configurado).");
            }
            else
            {
                _logger.LogWarning(
                    "WebPushSender no-op: VAPID_PUBLIC_KEY/VAPID_PRIVATE_KEY ausentes. " +
                    "Gere o par com VapidHelper.GenerateVapidKeys() (em dev) ou ferramenta equivalente.");
            }
        }

        public bool Ativo { get; }
        public string ChavePublica { get; }

        public async Task NotificarTenantAsync(int tenantId, string titulo, string corpo, string url = null)
        {
            if (!Ativo) return;

            using var scope = _scopes.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IWebPushSubscriptionRepository>();
            var subs = (await repo.GetByTenantAsync(tenantId)).ToList();
            if (subs.Count == 0) return;

            var payload = JsonSerializer.Serialize(new
            {
                title = titulo,
                body = corpo,
                url = url ?? "/admin/dashboard"
            });

            foreach (var s in subs)
            {
                var pushSub = new WebPushLib.PushSubscription(s.PushEndpoint, s.PushP256dh, s.PushAuth);
                try
                {
                    await _client.SendNotificationAsync(pushSub, payload, _vapid);
                    s.MarcarEnvioRealizado();
                    await repo.UpdateAsync(s);
                }
                catch (WebPushLib.WebPushException ex)
                    when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // Subscription expirou no browser — limpa do banco
                    _logger.LogInformation("Removendo subscription expirada {Endpoint}", s.PushEndpoint);
                    await repo.DeleteByEndpointAsync(s.PushEndpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar Web Push para {Endpoint}", s.PushEndpoint);
                }
            }

            // Persiste UpdateAsync com SaveChanges via scope (repo só Mark; SaveChanges fica aqui)
            var uow = scope.ServiceProvider.GetRequiredService<Core.Interfaces.Database.Common.IUnitOfWork>();
            await uow.SaveChangesAsync();
        }
    }
}

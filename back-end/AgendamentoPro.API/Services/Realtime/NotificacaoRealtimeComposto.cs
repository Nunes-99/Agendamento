using AgendamentoPro.Core.Interfaces.Services;

namespace AgendamentoPro.API.Services.Realtime
{
    /// <summary>
    /// Decorator: cada evento dispara SignalR (admin com aba aberta) E Web Push
    /// (admin com aba fechada ou em outro dispositivo). SignalR é fire-and-forget
    /// dentro do controller; Web Push é stream paralelo, e a falha de um não
    /// derruba o outro.
    /// </summary>
    public class NotificacaoRealtimeComposto : INotificacaoRealtime
    {
        private readonly SignalRNotificacaoRealtime _signalR;
        private readonly IWebPushSender _push;

        public NotificacaoRealtimeComposto(SignalRNotificacaoRealtime signalR, IWebPushSender push)
        {
            _signalR = signalR;
            _push = push;
        }

        public async Task NotificarTenantAsync(int tenantId, string evento, object payload)
        {
            var signalRTask = _signalR.NotificarTenantAsync(tenantId, evento, payload);

            // Mapeia evento → mensagem amigável pro Web Push
            var (titulo, corpo, url) = MapearMensagem(evento, payload);
            var pushTask = _push.Ativo
                ? _push.NotificarTenantAsync(tenantId, titulo, corpo, url)
                : Task.CompletedTask;

            await Task.WhenAll(signalRTask, pushTask);
        }

        private static (string titulo, string corpo, string url) MapearMensagem(string evento, object payload)
        {
            // Lê propriedades opcionais do payload via reflexão pra montar mensagem
            // sem acoplar a tipos específicos. Cada use case define seu payload.
            var t = payload.GetType();
            string GetStr(string nome) => t.GetProperty(nome)?.GetValue(payload)?.ToString();

            return evento switch
            {
                "novo-agendamento" => (
                    "Novo agendamento",
                    $"{GetStr("clienteNome")} marcou {GetStr("servicoNome")}",
                    "/admin/agenda"),
                "pagamento-aprovado" => (
                    "Pagamento aprovado",
                    $"Agendamento #{GetStr("agendamentoId")} confirmado",
                    "/admin/agenda"),
                "agendamento-cancelado" => (
                    "Agendamento cancelado",
                    $"Motivo: {GetStr("motivo") ?? "não informado"}",
                    "/admin/agenda"),
                _ => ($"Notificação ({evento})", "", "/admin/dashboard")
            };
        }
    }
}

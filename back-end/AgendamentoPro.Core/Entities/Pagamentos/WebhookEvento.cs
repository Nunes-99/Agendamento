namespace AgendamentoPro.Core.Entities.Pagamentos
{
    /// <summary>
    /// Registro de eventos de webhook recebidos. Garante idempotência:
    /// uma vez gravado (Gateway, EventoId), tentativas duplicadas (retries do gateway)
    /// caem na unique-constraint e podem ser ignoradas com segurança.
    /// </summary>
    public class WebhookEvento
    {
        public int WhEvId { get; private set; }
        public string WhEvGateway { get; private set; }
        public string WhEvEventoId { get; private set; }
        public string WhEvTipo { get; private set; }
        public DateTime WhEvRecebidoEm { get; private set; }
        public DateTime? WhEvProcessadoEm { get; private set; }
        public string WhEvPayload { get; private set; }

        protected WebhookEvento() { }

        public WebhookEvento(string gateway, string eventoId, string tipo, string payload)
        {
            WhEvGateway = gateway;
            WhEvEventoId = eventoId;
            WhEvTipo = tipo;
            WhEvPayload = payload;
            WhEvRecebidoEm = DateTime.UtcNow;
        }

        public void MarcarProcessado() => WhEvProcessadoEm = DateTime.UtcNow;
    }
}

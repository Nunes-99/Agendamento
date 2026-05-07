using AgendamentoPro.Core.Enums;

namespace AgendamentoPro.Core.Interfaces.Services
{
    public class CobrancaResult
    {
        public string GatewayId { get; set; }
        public string QrCode { get; set; }
        public string LinkPagamento { get; set; }
        public DateTime Expiracao { get; set; }
        public string PayloadBruto { get; set; }
    }

    public class WebhookEvent
    {
        /// <summary>Id do registro no gateway (ex: payment id no Mercado Pago).</summary>
        public string GatewayId { get; set; }
        /// <summary>Id único da notificação (top-level id do payload). Usado para idempotência.</summary>
        public string EventoId { get; set; }
        /// <summary>Tipo do evento (ex: "payment.updated"). Informativo.</summary>
        public string Tipo { get; set; }
        public StatusPagamento Status { get; set; }
        public string PayloadBruto { get; set; }
    }

    /// <summary>
    /// Abstração genérica para gateway de pagamento (Mercado Pago, Stripe, etc).
    /// </summary>
    public interface IGatewayPagamento
    {
        string Nome { get; }
        Task<CobrancaResult> CriarCobrancaAsync(int tenantId, int agendamentoId,
            decimal valor, FormaPagamento forma, string descricao, int expiracaoMinutos);
        Task<WebhookEvent> ProcessarWebhookAsync(string payload, string assinatura);
    }
}

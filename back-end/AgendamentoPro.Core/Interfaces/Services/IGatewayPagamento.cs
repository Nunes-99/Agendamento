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

        /// <summary>
        /// Indica se este gateway processa essa forma de pagamento. Usado pelo
        /// caller para escolher o gateway certo quando há múltiplos registrados
        /// (ex: PIX só MercadoPago; cartão internacional via Stripe).
        /// </summary>
        bool Suporta(FormaPagamento forma);

        /// <param name="payerEmail">
        /// E-mail do cliente pagador, quando informado. O Mercado Pago EXIGE um
        /// payer.email válido no PIX — o placeholder antigo (@agendamentopro.local)
        /// era rejeitado com 400 e nenhum PIX era criado.
        /// </param>
        Task<CobrancaResult> CriarCobrancaAsync(int tenantId, int agendamentoId,
            decimal valor, FormaPagamento forma, string descricao, int expiracaoMinutos,
            string payerEmail = null);
        Task<WebhookEvent> ProcessarWebhookAsync(string payload, string assinatura);
    }
}

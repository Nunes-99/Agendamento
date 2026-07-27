namespace AgendamentoPro.Core.Interfaces.Services
{
    public class CriarAssinaturaGatewayResult
    {
        public string PreapprovalId { get; set; }
        public string InitPointUrl { get; set; }
        public DateTime? ProximoVencimento { get; set; }
        public string PayloadBruto { get; set; }
    }

    public enum TipoEventoAssinatura
    {
        /// <summary>Cliente autorizou o preapproval (cartão validado).</summary>
        PreapprovalAutorizado,
        /// <summary>Preapproval pausado.</summary>
        PreapprovalPausado,
        /// <summary>Preapproval cancelado (pelo cliente ou por nós via PUT).</summary>
        PreapprovalCancelado,
        /// <summary>Cobrança mensal aprovada.</summary>
        PagamentoAprovado,
        /// <summary>Cobrança mensal recusada / falha.</summary>
        PagamentoRecusado,
        /// <summary>Cobrança mensal estornada.</summary>
        PagamentoEstornado,
        /// <summary>Outro evento (informativo, ignorar).</summary>
        Outro
    }

    public class WebhookAssinaturaEvent
    {
        /// <summary>Id único da notificação. Chave de idempotência.</summary>
        public string EventoId { get; set; }
        public TipoEventoAssinatura Tipo { get; set; }
        /// <summary>ID do preapproval no gateway. Sempre presente.</summary>
        public string PreapprovalId { get; set; }
        /// <summary>ID do pagamento individual (para eventos de PagamentoAprovado/Recusado).</summary>
        public string PaymentId { get; set; }
        public decimal? Valor { get; set; }
        public DateTime? OcorreuEm { get; set; }
        public DateTime? ProximoVencimento { get; set; }
        public string PayloadBruto { get; set; }
    }

    /// <summary>
    /// Abstração para gateway de cobrança recorrente (mensalidade SaaS do tenant).
    /// Separado de IGatewayPagamento (transacional do cliente final).
    /// </summary>
    public interface IGatewayAssinatura
    {
        string Nome { get; }

        /// <summary>
        /// Cria um preapproval (subscription) no gateway. O usuário deve ser redirecionado para
        /// InitPointUrl para autorizar o débito recorrente (cadastrar cartão).
        ///
        /// <paramref name="trialMeses"/> é o período grátis: o cartão é autorizado, mas a
        /// primeira cobrança só acontece depois desse número de meses. Zero = cobra já no
        /// primeiro ciclo. O mesmo valor governa o status Trial local, para os dois lados
        /// concordarem sobre quando a cobrança começa.
        /// </summary>
        Task<CriarAssinaturaGatewayResult> CriarPreapprovalAsync(
            int tenantId, int assinaturaId, decimal valor, string descricao,
            string payerEmail, string backUrl, int trialMeses);

        /// <summary>Cancela um preapproval ativo. Idempotente.</summary>
        Task<bool> CancelarAsync(string preapprovalId);

        /// <summary>Altera o valor mensal de um preapproval ativo (mudança de plano).</summary>
        Task<bool> AtualizarValorAsync(string preapprovalId, decimal novoValor);

        /// <summary>Processa webhook de assinatura (assinatura_preapproval ou authorized_payment).</summary>
        Task<WebhookAssinaturaEvent> ProcessarWebhookAsync(string payload, string assinatura);
    }
}

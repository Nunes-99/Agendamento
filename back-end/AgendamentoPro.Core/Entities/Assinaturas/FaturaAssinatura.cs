using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Assinaturas
{
    /// <summary>
    /// Histórico de cobranças individuais de uma Assinatura (uma por ciclo de cobrança mensal).
    /// </summary>
    public class FaturaAssinatura : ITenantScoped
    {
        public int FasId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_AssId { get; private set; }
        public decimal FasValor { get; private set; }
        public StatusFaturaAssinatura FasStatus { get; private set; }
        public string FasGatewayPaymentId { get; private set; }
        public DateTime FasReferenciaInicio { get; private set; }
        public DateTime FasReferenciaFim { get; private set; }
        public DateTime FasVencimentoEm { get; private set; }
        public DateTime? FasPagoEm { get; private set; }
        public string FasPayloadGateway { get; private set; }
        public DateTime FasCriadoEm { get; private set; }

        public Assinatura Assinatura { get; private set; }

        protected FaturaAssinatura() { }

        public FaturaAssinatura(int rTenId, int rAssId, decimal valor,
            DateTime referenciaInicio, DateTime referenciaFim, DateTime vencimentoEm)
        {
            R_TenId = rTenId;
            R_AssId = rAssId;
            FasValor = valor;
            FasStatus = StatusFaturaAssinatura.Pendente;
            FasReferenciaInicio = referenciaInicio;
            FasReferenciaFim = referenciaFim;
            FasVencimentoEm = vencimentoEm;
            FasCriadoEm = DateTime.UtcNow;

            if (valor <= 0) throw new DomainException("Valor da fatura deve ser positivo.");
            if (referenciaFim < referenciaInicio) throw new DomainException("Período de referência inválido.");
        }

        public void DefinirGatewayPaymentId(string gatewayPaymentId, string payload = null)
        {
            FasGatewayPaymentId = gatewayPaymentId;
            if (!string.IsNullOrEmpty(payload)) FasPayloadGateway = payload;
        }

        public bool Pagar(DateTime pagoEm, string payload = null)
        {
            if (FasStatus == StatusFaturaAssinatura.Paga) return false;
            FasStatus = StatusFaturaAssinatura.Paga;
            FasPagoEm = pagoEm;
            if (!string.IsNullOrEmpty(payload)) FasPayloadGateway = payload;
            return true;
        }

        public bool Recusar(string payload = null)
        {
            if (FasStatus == StatusFaturaAssinatura.Recusada) return false;
            FasStatus = StatusFaturaAssinatura.Recusada;
            if (!string.IsNullOrEmpty(payload)) FasPayloadGateway = payload;
            return true;
        }

        public bool Estornar()
        {
            if (FasStatus == StatusFaturaAssinatura.Estornada) return false;
            FasStatus = StatusFaturaAssinatura.Estornada;
            return true;
        }
    }
}

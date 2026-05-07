using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Pagamentos
{
    public class Pagamento : ITenantScoped
    {
        public int PagId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_AgeId { get; private set; }
        public FormaPagamento PagForma { get; private set; }
        public StatusPagamento PagStatus { get; private set; }
        public decimal PagValor { get; private set; }
        public string PagGateway { get; private set; }
        public string PagGatewayId { get; private set; }
        public string PagQrCode { get; private set; }
        public string PagLinkPagamento { get; private set; }
        public DateTime? PagExpiracao { get; private set; }
        public DateTime? PagAprovadoEm { get; private set; }
        public string PagPayloadGateway { get; private set; }
        public DateTime PagCriadoEm { get; private set; }

        public Tenant Tenant { get; private set; }
        public Agendamento Agendamento { get; private set; }

        protected Pagamento() { }

        public Pagamento(int rTenId, int rAgeId, FormaPagamento forma, decimal valor,
            string gateway, DateTime? expiracao)
        {
            R_TenId = rTenId;
            R_AgeId = rAgeId;
            PagForma = forma;
            PagValor = valor;
            PagGateway = gateway;
            PagExpiracao = expiracao;
            PagStatus = StatusPagamento.Pendente;
            PagCriadoEm = DateTime.UtcNow;

            if (valor <= 0)
                throw new DomainException("Valor do pagamento deve ser positivo.");
        }

        public void DefinirDadosGateway(string gatewayId, string qrCode, string linkPagamento, string payload)
        {
            PagGatewayId = gatewayId;
            PagQrCode = qrCode;
            PagLinkPagamento = linkPagamento;
            PagPayloadGateway = payload;
        }

        /// <summary>
        /// Marca como aprovado. Idempotente: se já estava aprovado, retorna false e não altera timestamp.
        /// </summary>
        public bool Aprovar(string payload = null)
        {
            if (PagStatus == StatusPagamento.Aprovado) return false;
            PagStatus = StatusPagamento.Aprovado;
            PagAprovadoEm = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(payload))
                PagPayloadGateway = payload;
            return true;
        }

        public bool Recusar()
        {
            if (PagStatus == StatusPagamento.Recusado) return false;
            PagStatus = StatusPagamento.Recusado;
            return true;
        }

        public bool Estornar()
        {
            if (PagStatus == StatusPagamento.Estornado) return false;
            PagStatus = StatusPagamento.Estornado;
            return true;
        }

        public bool Expirar()
        {
            if (PagStatus == StatusPagamento.Expirado) return false;
            PagStatus = StatusPagamento.Expirado;
            return true;
        }
    }
}

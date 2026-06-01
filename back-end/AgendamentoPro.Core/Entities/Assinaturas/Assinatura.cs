using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Assinaturas
{
    /// <summary>
    /// Assinatura SaaS de um tenant (1 ativa por tenant).
    /// Diferente de Pagamento (transacional do cliente final que agenda serviço).
    /// </summary>
    public class Assinatura : ITenantScoped
    {
        public int AssId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_PlnId { get; private set; }
        public StatusAssinatura AssStatus { get; private set; }
        public string AssGateway { get; private set; }
        public string AssGatewayPreapprovalId { get; private set; }
        public DateTime AssDataInicio { get; private set; }
        public DateTime? AssTrialAteEm { get; private set; }
        public DateTime? AssProximoVencimento { get; private set; }
        public DateTime? AssUltimoPagamentoEm { get; private set; }
        public DateTime? AssAtrasoDesde { get; private set; }
        public DateTime? AssReadOnlyDesde { get; private set; }
        public DateTime? AssCanceladaEm { get; private set; }
        public DateTime? AssExpiradaEm { get; private set; }
        public string AssPayloadGateway { get; private set; }
        public DateTime AssCriadoEm { get; private set; }

        public Tenant Tenant { get; private set; }
        public Plano Plano { get; private set; }

        protected Assinatura() { }

        public Assinatura(int rTenId, int rPlnId, string gateway, DateTime? trialAteEm = null)
        {
            R_TenId = rTenId;
            R_PlnId = rPlnId;
            AssGateway = gateway;
            AssDataInicio = DateTime.UtcNow;
            AssTrialAteEm = trialAteEm;
            AssStatus = trialAteEm.HasValue ? StatusAssinatura.Trial : StatusAssinatura.Ativa;
            AssCriadoEm = DateTime.UtcNow;

            if (rTenId <= 0) throw new DomainException("TenantId da assinatura é obrigatório.");
            if (rPlnId <= 0) throw new DomainException("PlanoId da assinatura é obrigatório.");
            if (string.IsNullOrWhiteSpace(gateway)) throw new DomainException("Gateway da assinatura é obrigatório.");
        }

        public void DefinirPreapproval(string preapprovalId, DateTime proximoVencimento, string payload = null)
        {
            AssGatewayPreapprovalId = preapprovalId;
            AssProximoVencimento = proximoVencimento;
            if (!string.IsNullOrEmpty(payload)) AssPayloadGateway = payload;
        }

        /// <summary>
        /// Registra pagamento aprovado: limpa estados de atraso e avança próximo vencimento.
        /// Idempotente: se a data já consta como último pagamento, retorna false.
        /// </summary>
        public bool RegistrarPagamento(DateTime pagoEm, DateTime proximoVencimento)
        {
            if (AssUltimoPagamentoEm.HasValue && AssUltimoPagamentoEm.Value == pagoEm) return false;
            AssUltimoPagamentoEm = pagoEm;
            AssProximoVencimento = proximoVencimento;
            AssAtrasoDesde = null;
            AssReadOnlyDesde = null;
            AssStatus = StatusAssinatura.Ativa;
            return true;
        }

        /// <summary>D+0: cobrança falhou. Marca como Atrasada (acesso total continua).</summary>
        public bool MarcarAtrasada(DateTime quando)
        {
            if (AssStatus == StatusAssinatura.Atrasada || AssStatus == StatusAssinatura.ReadOnly) return false;
            if (AssStatus == StatusAssinatura.Cancelada || AssStatus == StatusAssinatura.Expirada) return false;
            AssStatus = StatusAssinatura.Atrasada;
            AssAtrasoDesde = quando;
            return true;
        }

        /// <summary>D+8: transiciona pra ReadOnly. Acesso de escrita bloqueado.</summary>
        public bool TransicionarReadOnly(DateTime quando)
        {
            if (AssStatus == StatusAssinatura.ReadOnly) return false;
            if (AssStatus != StatusAssinatura.Atrasada) return false;
            AssStatus = StatusAssinatura.ReadOnly;
            AssReadOnlyDesde = quando;
            return true;
        }

        /// <summary>D+30: expira definitivamente. Quem dispara isso deve também soft deletar o Tenant.</summary>
        public bool Expirar(DateTime quando)
        {
            if (AssStatus == StatusAssinatura.Expirada) return false;
            if (AssStatus != StatusAssinatura.ReadOnly) return false;
            AssStatus = StatusAssinatura.Expirada;
            AssExpiradaEm = quando;
            return true;
        }

        public bool Cancelar(DateTime quando)
        {
            if (AssStatus == StatusAssinatura.Cancelada || AssStatus == StatusAssinatura.Expirada) return false;
            AssStatus = StatusAssinatura.Cancelada;
            AssCanceladaEm = quando;
            return true;
        }

        public bool AlterarPlano(int novoPlanoId)
        {
            if (novoPlanoId <= 0) throw new DomainException("PlanoId inválido.");
            if (R_PlnId == novoPlanoId) return false;
            R_PlnId = novoPlanoId;
            return true;
        }

        /// <summary>Resumo binário pro middleware/guard: pode executar operações de escrita?</summary>
        public bool PermiteEscrita() =>
            AssStatus == StatusAssinatura.Trial ||
            AssStatus == StatusAssinatura.Ativa ||
            AssStatus == StatusAssinatura.Atrasada;
    }
}

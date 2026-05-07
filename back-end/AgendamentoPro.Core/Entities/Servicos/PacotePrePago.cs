using AgendamentoPro.Core.Entities.Common;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Servicos
{
    /// <summary>
    /// Cliente compra pacote de N atendimentos do mesmo serviço (ex: 5 lavagens),
    /// paga upfront, usa em qualquer agendamento futuro até consumir todos.
    /// Entidade de catálogo: tenant cadastra o pacote; ClienteSaldo registra a compra.
    /// </summary>
    public class PacotePrePago : SoftDeletableEntity, ITenantScoped
    {
        public int PctId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_SerId { get; private set; }
        public string PctNome { get; private set; }
        public int PctQuantidade { get; private set; }
        public decimal PctPreco { get; private set; }
        public int PctValidadeDias { get; private set; }
        public bool PctAtivo { get; private set; }
        public DateTime PctCriadoEm { get; private set; }

        public Tenant Tenant { get; private set; }
        public Servico Servico { get; private set; }

        protected PacotePrePago() { }

        public PacotePrePago(int rTenId, int rSerId, string nome, int quantidade,
            decimal preco, int validadeDias)
        {
            if (rTenId <= 0 || rSerId <= 0) throw new DomainException("Tenant e serviço obrigatórios.");
            if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("Nome obrigatório.");
            if (quantidade < 2) throw new DomainException("Quantidade mínima de 2.");
            if (preco <= 0) throw new DomainException("Preço deve ser positivo.");
            if (validadeDias < 1) throw new DomainException("Validade mínima de 1 dia.");

            R_TenId = rTenId;
            R_SerId = rSerId;
            PctNome = nome;
            PctQuantidade = quantidade;
            PctPreco = preco;
            PctValidadeDias = validadeDias;
            PctAtivo = true;
            PctCriadoEm = DateTime.UtcNow;
        }
    }

    public enum StatusSaldoPacote { Pendente = 0, Ativo = 1, Cancelado = 2 }

    /// <summary>
    /// Saldo de pacote pré-pago de um cliente. Cliente compra → saldo fica Pendente
    /// até o webhook do gateway aprovar o pagamento → Ativo. Só Ativo permite débito.
    /// </summary>
    public class SaldoPacote : ITenantScoped
    {
        public int SaldId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_CliId { get; private set; }
        public int R_PctId { get; private set; }
        public int SaldQuantidadeRestante { get; private set; }
        public DateTime SaldExpiraEm { get; private set; }
        public DateTime SaldCriadoEm { get; private set; }
        public StatusSaldoPacote SaldStatus { get; private set; }
        public string SaldGatewayPagamentoId { get; private set; }
        public DateTime? SaldPagoEm { get; private set; }

        public Tenant Tenant { get; private set; }
        public PacotePrePago Pacote { get; private set; }

        protected SaldoPacote() { }

        public SaldoPacote(int rTenId, int rCliId, PacotePrePago pacote)
        {
            R_TenId = rTenId;
            R_CliId = rCliId;
            R_PctId = pacote.PctId;
            SaldQuantidadeRestante = pacote.PctQuantidade;
            SaldExpiraEm = DateTime.UtcNow.AddDays(pacote.PctValidadeDias);
            SaldCriadoEm = DateTime.UtcNow;
            SaldStatus = StatusSaldoPacote.Pendente; // só vira Ativo após webhook aprovar
        }

        public void DefinirGatewayId(string gatewayId) => SaldGatewayPagamentoId = gatewayId;

        /// <summary>Idempotente — chamada repetida do webhook não duplica.</summary>
        public bool Ativar()
        {
            if (SaldStatus != StatusSaldoPacote.Pendente) return false;
            SaldStatus = StatusSaldoPacote.Ativo;
            SaldPagoEm = DateTime.UtcNow;
            return true;
        }

        public void Cancelar() => SaldStatus = StatusSaldoPacote.Cancelado;

        public bool PodeUsar() =>
            SaldStatus == StatusSaldoPacote.Ativo
            && SaldQuantidadeRestante > 0
            && DateTime.UtcNow < SaldExpiraEm;

        public bool Debitar()
        {
            if (!PodeUsar()) return false;
            SaldQuantidadeRestante--;
            return true;
        }
    }
}

using AgendamentoPro.Core.Enums;

namespace AgendamentoPro.Application.ViewModels.Agendamentos
{
    public class AgendamentoViewModel
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNome { get; set; }
        public string ClienteTelefone { get; set; }
        public int ServicoId { get; set; }
        public string ServicoNome { get; set; }
        public int RecursoId { get; set; }
        public string RecursoNome { get; set; }
        public DateTime Data { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }
        public StatusAgendamento Status { get; set; }
        public string StatusDescricao { get; set; }
        public StatusPagamento StatusPagamento { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal ValorEntrada { get; set; }
        public string Observacao { get; set; }
        public string MotivoCancelamento { get; set; }
        public DateTime CriadoEm { get; set; }
        /// <summary>Token público da avaliação - presente apenas após Concluir.</summary>
        public Guid? AvaliacaoToken { get; set; }
        /// <summary>Identificador do grupo combo - presente quando o agendamento veio de um combo.</summary>
        public Guid? GrupoComboId { get; set; }
    }

    public class CriarAgendamentoResultViewModel
    {
        public AgendamentoViewModel Agendamento { get; set; }
        public PagamentoViewModel Pagamento { get; set; }
    }

    public class PagamentoViewModel
    {
        public int Id { get; set; }
        public FormaPagamento Forma { get; set; }
        public StatusPagamento Status { get; set; }
        public decimal Valor { get; set; }
        public string QrCode { get; set; }
        public string LinkPagamento { get; set; }
        public DateTime? Expiracao { get; set; }
    }
}

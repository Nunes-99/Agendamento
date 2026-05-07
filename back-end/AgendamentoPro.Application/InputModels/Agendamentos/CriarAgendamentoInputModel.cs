using AgendamentoPro.Core.Enums;

namespace AgendamentoPro.Application.InputModels.Agendamentos
{
    public class CriarAgendamentoInputModel
    {
        public int ServicoId { get; set; }
        public int? RecursoId { get; set; }
        public DateTime Data { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public string Observacao { get; set; }
        public ClientePublicoInputModel Cliente { get; set; }
        public FormaPagamento FormaPagamento { get; set; }
    }

    public class ClientePublicoInputModel
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public string WhatsApp { get; set; }
        public string Cpf { get; set; }
    }

    public class ReagendarInputModel
    {
        public DateTime NovaData { get; set; }
        public TimeSpan NovaHoraInicio { get; set; }
    }

    public class CriarAgendamentoAdminInputModel
    {
        public int ServicoId { get; set; }
        public int? RecursoId { get; set; }
        public DateTime Data { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public string Observacao { get; set; }
        public int? ClienteId { get; set; }
        public ClientePublicoInputModel Cliente { get; set; }
        public decimal? Valor { get; set; }
    }

    public class CancelarAgendamentoInputModel
    {
        public string Motivo { get; set; }
    }
}

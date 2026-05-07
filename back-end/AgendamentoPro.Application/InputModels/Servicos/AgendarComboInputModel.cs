using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Core.Enums;

namespace AgendamentoPro.Application.InputModels.Servicos
{
    public class AgendarComboInputModel
    {
        public int? RecursoId { get; set; }
        public DateTime Data { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public string Observacao { get; set; }
        public ClientePublicoInputModel Cliente { get; set; }
        public FormaPagamento FormaPagamento { get; set; }
        public string CupomCodigo { get; set; }
    }
}

using AgendamentoPro.Core.Entities.Agendamentos;

namespace AgendamentoPro.Application.ViewModels.Agendamentos
{
    public class FotoAgendamentoViewModel
    {
        public int Id { get; set; }
        public int AgendamentoId { get; set; }
        public TipoFoto Tipo { get; set; }
        public string Url { get; set; }
        public string ContentType { get; set; }
        public long TamanhoBytes { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}

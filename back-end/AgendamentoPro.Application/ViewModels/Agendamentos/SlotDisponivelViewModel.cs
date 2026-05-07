namespace AgendamentoPro.Application.ViewModels.Agendamentos
{
    public class SlotDisponivelViewModel
    {
        public DateTime Data { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }
        public int RecursoId { get; set; }
        public string RecursoNome { get; set; }
    }
}

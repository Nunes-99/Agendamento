namespace AgendamentoPro.Core.Interfaces.Services
{
    public class SlotDisponivel
    {
        public DateTime Data { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }
        public int RecursoId { get; set; }
        public string RecursoNome { get; set; }
    }

    public interface IDisponibilidadeService
    {
        Task<IEnumerable<SlotDisponivel>> CalcularSlotsAsync(int tenantId, int servicoId, DateTime data, int? recursoId = null);
    }
}

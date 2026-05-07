using AgendamentoPro.Application.Interfaces.Agendamentos;
using AgendamentoPro.Application.ViewModels.Agendamentos;
using AgendamentoPro.Core.Interfaces.Services;

namespace AgendamentoPro.Application.UseCases.Agendamentos
{
    public class ConsultarSlotsUseCase : IConsultarSlotsUseCase
    {
        private readonly IDisponibilidadeService _disponibilidade;

        public ConsultarSlotsUseCase(IDisponibilidadeService disponibilidade)
        {
            _disponibilidade = disponibilidade;
        }

        public async Task<IEnumerable<SlotDisponivelViewModel>> ExecuteAsync(int tenantId, int servicoId, DateTime data, int? recursoId = null)
        {
            var slots = await _disponibilidade.CalcularSlotsAsync(tenantId, servicoId, data, recursoId);
            return slots.Select(s => new SlotDisponivelViewModel
            {
                Data = s.Data,
                HoraInicio = s.HoraInicio,
                HoraFim = s.HoraFim,
                RecursoId = s.RecursoId,
                RecursoNome = s.RecursoNome
            });
        }
    }
}

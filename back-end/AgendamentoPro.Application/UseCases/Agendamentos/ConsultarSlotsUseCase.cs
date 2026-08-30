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

        /// <summary>
        /// Percorre o período dia a dia. São várias consultas, mas ficam em UMA
        /// ida ao servidor: antes, a tela só sabia se um dia tinha vaga depois de
        /// escolhê-lo, e quem caía num domingo fechado via "nenhum horário
        /// disponível" sem nenhuma pista de onde procurar.
        /// </summary>
        public async Task<IEnumerable<DiaDisponivelViewModel>> DiasAsync(int tenantId, int servicoId,
            DateTime inicio, int dias, int? recursoId = null)
        {
            dias = Math.Clamp(dias, 1, 60);
            var resultado = new List<DiaDisponivelViewModel>(dias);

            for (var i = 0; i < dias; i++)
            {
                var data = inicio.Date.AddDays(i);
                var slots = (await _disponibilidade.CalcularSlotsAsync(tenantId, servicoId, data, recursoId))
                    .ToList();
                resultado.Add(new DiaDisponivelViewModel
                {
                    Data = data,
                    // HORÁRIOS distintos, não linhas: com 4 boxes o mesmo 08:00
                    // aparece 4 vezes na lista de slots, e "136 vagas" num dia de
                    // 8 horas só assusta quem lê.
                    Vagas = slots.Select(s => s.HoraInicio).Distinct().Count(),
                    PrimeiroHorario = slots.Count == 0 ? null : slots.Min(s => s.HoraInicio)
                });
            }
            return resultado;
        }
    }
}

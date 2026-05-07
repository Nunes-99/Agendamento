using AgendamentoPro.Application.Interfaces.Relatorios;
using AgendamentoPro.Application.ViewModels.Relatorios;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Relatorios
{
    public class RelatoriosUseCase : IRelatoriosUseCase
    {
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IRecursoRepository _recursos;
        private readonly IHorarioFuncionamentoRepository _horarios;

        public RelatoriosUseCase(IAgendamentoRepository a, IRecursoRepository r,
            IHorarioFuncionamentoRepository h)
        {
            _agendamentos = a;
            _recursos = r;
            _horarios = h;
        }

        public async Task<IEnumerable<ReceitaPeriodoViewModel>> ReceitaPorDiaAsync(int tenantId, DateTime inicio, DateTime fim)
        {
            var lista = await _agendamentos.GetByPeriodoAsync(tenantId, inicio, fim);
            return lista
                .Where(a => a.AgeStatus == StatusAgendamento.Concluido)
                .GroupBy(a => a.AgeData.Date)
                .Select(g => new ReceitaPeriodoViewModel
                {
                    Data = g.Key,
                    Receita = g.Sum(x => x.AgeValorTotal),
                    Quantidade = g.Count()
                })
                .OrderBy(x => x.Data);
        }

        public async Task<IEnumerable<ServicoMaisVendidoViewModel>> ServicosMaisVendidosAsync(int tenantId, DateTime inicio, DateTime fim)
        {
            var lista = await _agendamentos.GetByPeriodoAsync(tenantId, inicio, fim);
            return lista
                .Where(a => a.Servico != null && a.AgeStatus == StatusAgendamento.Concluido)
                .GroupBy(a => new { a.R_SerId, a.Servico.SerNome })
                .Select(g => new ServicoMaisVendidoViewModel
                {
                    ServicoId = g.Key.R_SerId,
                    Nome = g.Key.SerNome,
                    Quantidade = g.Count(),
                    ReceitaTotal = g.Sum(x => x.AgeValorTotal)
                })
                .OrderByDescending(x => x.Quantidade);
        }

        public async Task<IEnumerable<TaxaOcupacaoViewModel>> TaxaOcupacaoAsync(int tenantId, DateTime inicio, DateTime fim)
        {
            var recursos = await _recursos.GetByTenantAsync(tenantId, true);
            var horarios = (await _horarios.GetByTenantAsync(tenantId)).ToDictionary(h => h.HorDiaSemana);
            var lista = (await _agendamentos.GetByPeriodoAsync(tenantId, inicio, fim)).ToList();

            int minutosFuncionamentoPorDia(DateTime dia)
            {
                if (!horarios.TryGetValue(dia.DayOfWeek, out var h) || !h.HorAberto) return 0;
                var total = (h.HorFechamento - h.HorAbertura).TotalMinutes;
                if (h.HorPausaInicio.HasValue && h.HorPausaFim.HasValue)
                    total -= (h.HorPausaFim.Value - h.HorPausaInicio.Value).TotalMinutes;
                return (int)Math.Max(0, total);
            }

            var minutosTotais = 0;
            for (var d = inicio.Date; d <= fim.Date; d = d.AddDays(1))
                minutosTotais += minutosFuncionamentoPorDia(d);

            return recursos.Select(r =>
            {
                var ocupados = lista
                    .Where(a => a.R_RecId == r.RecId
                        && a.AgeStatus != StatusAgendamento.Cancelado
                        && a.AgeStatus != StatusAgendamento.NoShow)
                    .Sum(a => (a.AgeHoraFim - a.AgeHoraInicio).TotalMinutes);

                return new TaxaOcupacaoViewModel
                {
                    RecursoId = r.RecId,
                    RecursoNome = r.RecNome,
                    SlotsTotais = minutosTotais,
                    SlotsOcupados = (int)ocupados
                };
            });
        }

        public async Task<IEnumerable<CancelamentoViewModel>> CancelamentosAsync(int tenantId, DateTime inicio, DateTime fim)
        {
            var lista = await _agendamentos.GetByPeriodoAsync(tenantId, inicio, fim);
            return lista
                .Where(a => a.AgeStatus == StatusAgendamento.Cancelado)
                .GroupBy(a => a.AgeData.Date)
                .Select(g => new CancelamentoViewModel
                {
                    Data = g.Key,
                    Quantidade = g.Count(),
                    MotivoMaisComum = g.GroupBy(x => x.AgeMotivoCancelamento ?? "Não informado")
                        .OrderByDescending(x => x.Count()).First().Key
                });
        }
    }
}

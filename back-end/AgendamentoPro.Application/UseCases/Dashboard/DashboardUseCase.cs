using AgendamentoPro.Application.Interfaces.Dashboard;
using AgendamentoPro.Application.ViewModels.Dashboard;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Dashboard
{
    public class DashboardUseCase : IDashboardUseCase
    {
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IServicoRepository _servicos;

        public DashboardUseCase(IAgendamentoRepository a, IServicoRepository s)
        {
            _agendamentos = a;
            _servicos = s;
        }

        public async Task<DashboardViewModel> ExecuteAsync(int tenantId)
        {
            var hoje = DateTime.Today;
            var inicioSemana = hoje.AddDays(-(int)hoje.DayOfWeek);
            var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);

            var agsHoje = await _agendamentos.GetByPeriodoAsync(tenantId, hoje, hoje.AddDays(1).AddTicks(-1));
            var agsSemana = await _agendamentos.GetByPeriodoAsync(tenantId, inicioSemana, inicioSemana.AddDays(7));
            var agsMes = await _agendamentos.GetByPeriodoAsync(tenantId, inicioMes, inicioMes.AddMonths(1));

            var receitaHoje = agsHoje.Where(a => a.AgeStatus == StatusAgendamento.Concluido).Sum(a => a.AgeValorTotal);
            var receitaMes = agsMes.Where(a => a.AgeStatus == StatusAgendamento.Concluido).Sum(a => a.AgeValorTotal);
            var pendentes = agsMes.Count(a => a.AgePagamentoStatus == StatusPagamento.Pendente);

            var topServicos = agsMes
                .Where(a => a.Servico != null)
                .GroupBy(a => new { a.R_SerId, a.Servico.SerNome })
                .Select(g => new TopServicoViewModel
                {
                    Nome = g.Key.SerNome,
                    Quantidade = g.Count(),
                    ReceitaTotal = g.Sum(x => x.AgeValorTotal)
                })
                .OrderByDescending(t => t.Quantidade)
                .Take(5)
                .ToList();

            var proximos = agsHoje
                .Where(a => a.AgeStatus == StatusAgendamento.Confirmado || a.AgeStatus == StatusAgendamento.PendentePagamento)
                .OrderBy(a => a.AgeHoraInicio)
                .Take(5)
                .Select(a => new AgendamentoResumoViewModel
                {
                    Id = a.AgeId,
                    Cliente = a.Cliente?.CliNome,
                    Servico = a.Servico?.SerNome,
                    Data = a.AgeData,
                    Hora = a.AgeHoraInicio,
                    Status = a.AgeStatus.ToString()
                }).ToList();

            return new DashboardViewModel
            {
                AgendamentosHoje = agsHoje.Count(),
                AgendamentosSemana = agsSemana.Count(),
                AgendamentosMes = agsMes.Count(),
                ReceitaHoje = receitaHoje,
                ReceitaMes = receitaMes,
                PendentesPagamento = pendentes,
                TopServicos = topServicos,
                ProximosAgendamentos = proximos
            };
        }
    }
}

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

        public async Task<IEnumerable<LtvClienteViewModel>> LtvClientesAsync(int tenantId, DateTime inicio, DateTime fim, int top = 20)
        {
            if (top <= 0) top = 20;
            var lista = await _agendamentos.GetByPeriodoAsync(tenantId, inicio, fim);
            return lista
                .Where(a => a.AgeStatus == StatusAgendamento.Concluido && a.Cliente != null)
                .GroupBy(a => new { a.R_CliId, a.Cliente.CliNome, a.Cliente.CliTelefone })
                .Select(g => new LtvClienteViewModel
                {
                    ClienteId = g.Key.R_CliId,
                    Nome = g.Key.CliNome,
                    Telefone = g.Key.CliTelefone,
                    QuantidadeAgendamentos = g.Count(),
                    ReceitaTotal = g.Sum(x => x.AgeValorTotal),
                    PrimeiroAgendamento = g.Min(x => x.AgeData),
                    UltimoAgendamento = g.Max(x => x.AgeData)
                })
                .OrderByDescending(x => x.ReceitaTotal)
                .Take(top)
                .ToList();
        }

        private static readonly string[] DiasSemanaPtBr =
            { "Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado" };

        public async Task<IEnumerable<NoShowViewModel>> NoShowPorDiaSemanaAsync(int tenantId, DateTime inicio, DateTime fim)
        {
            var lista = await _agendamentos.GetByPeriodoAsync(tenantId, inicio, fim);
            var relevantes = lista
                .Where(a => a.AgeStatus == StatusAgendamento.NoShow || a.AgeStatus == StatusAgendamento.Concluido)
                .ToList();

            // Retorna todos os 7 dias mesmo quando vazios — facilita o gráfico no front
            return Enumerable.Range(0, 7).Select(d =>
            {
                var doDia = relevantes.Where(a => (int)a.AgeData.DayOfWeek == d).ToList();
                return new NoShowViewModel
                {
                    Bucket = DiasSemanaPtBr[d],
                    NoShow = doDia.Count(a => a.AgeStatus == StatusAgendamento.NoShow),
                    Concluidos = doDia.Count(a => a.AgeStatus == StatusAgendamento.Concluido)
                };
            }).ToList();
        }

        public async Task<IEnumerable<NoShowViewModel>> NoShowPorHoraAsync(int tenantId, DateTime inicio, DateTime fim)
        {
            var lista = await _agendamentos.GetByPeriodoAsync(tenantId, inicio, fim);
            var relevantes = lista
                .Where(a => a.AgeStatus == StatusAgendamento.NoShow || a.AgeStatus == StatusAgendamento.Concluido)
                .ToList();

            return Enumerable.Range(0, 24).Select(h =>
            {
                var noBucket = relevantes.Where(a => a.AgeHoraInicio.Hours == h).ToList();
                return new NoShowViewModel
                {
                    Bucket = $"{h:D2}h",
                    NoShow = noBucket.Count(a => a.AgeStatus == StatusAgendamento.NoShow),
                    Concluidos = noBucket.Count(a => a.AgeStatus == StatusAgendamento.Concluido)
                };
            })
            .Where(v => v.Total > 0) // hora sem nenhum agendamento polui o gráfico
            .ToList();
        }

        public async Task<IEnumerable<SazonalidadeMesViewModel>> SazonalidadeMensalAsync(int tenantId, int meses = 12)
        {
            if (meses <= 0 || meses > 60) meses = 12;
            var inicio = DateTime.Today.AddMonths(-(meses - 1)).AddDays(-(DateTime.Today.Day - 1));
            var fim = DateTime.Today;
            var lista = await _agendamentos.GetByPeriodoAsync(tenantId, inicio, fim);

            var agrupado = lista
                .Where(a => a.AgeStatus == StatusAgendamento.Concluido)
                .GroupBy(a => new { a.AgeData.Year, a.AgeData.Month })
                .ToDictionary(g => (g.Key.Year, g.Key.Month), g => new SazonalidadeMesViewModel
                {
                    Ano = g.Key.Year,
                    Mes = g.Key.Month,
                    Receita = g.Sum(x => x.AgeValorTotal),
                    Quantidade = g.Count()
                });

            // Preenche meses sem agendamento com zeros — gráfico contínuo
            var resultado = new List<SazonalidadeMesViewModel>(meses);
            for (var i = 0; i < meses; i++)
            {
                var dt = inicio.AddMonths(i);
                resultado.Add(agrupado.TryGetValue((dt.Year, dt.Month), out var existente)
                    ? existente
                    : new SazonalidadeMesViewModel { Ano = dt.Year, Mes = dt.Month });
            }
            return resultado;
        }
    }
}

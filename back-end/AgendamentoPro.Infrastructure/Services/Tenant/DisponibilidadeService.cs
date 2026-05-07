using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;

namespace AgendamentoPro.Infrastructure.Services.Tenant
{
    /// <summary>
    /// Calcula slots disponíveis com base no horário de funcionamento, duração do serviço,
    /// buffer entre atendimentos, agendamentos existentes e bloqueios.
    /// </summary>
    public class DisponibilidadeService : IDisponibilidadeService
    {
        private readonly ITenantRepository _tenants;
        private readonly IServicoRepository _servicos;
        private readonly IRecursoRepository _recursos;
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IHorarioFuncionamentoRepository _horarios;

        public DisponibilidadeService(ITenantRepository tenants, IServicoRepository servicos,
            IRecursoRepository recursos, IAgendamentoRepository agendamentos,
            IHorarioFuncionamentoRepository horarios)
        {
            _tenants = tenants;
            _servicos = servicos;
            _recursos = recursos;
            _agendamentos = agendamentos;
            _horarios = horarios;
        }

        public async Task<IEnumerable<SlotDisponivel>> CalcularSlotsAsync(int tenantId, int servicoId, DateTime data, int? recursoId = null)
        {
            var tenant = await _tenants.GetByIdAsync(tenantId)
                ?? throw new TenantException("Estabelecimento não encontrado.");
            var servico = await _servicos.GetByIdAsync(servicoId, tenantId)
                ?? throw new ServicoException("Serviço não encontrado.");

            var horario = await _horarios.GetByDiaAsync(tenantId, data.DayOfWeek);
            if (horario == null || !horario.HorAberto)
                return Enumerable.Empty<SlotDisponivel>();

            var bloqueios = (await _horarios.GetBloqueiosAsync(tenantId,
                data.Date, data.Date.AddDays(1).AddTicks(-1))).ToList();

            var recursos = recursoId.HasValue
                ? new[] { await _recursos.GetByIdAsync(recursoId.Value, tenantId) }
                : (await _recursos.GetByTenantAsync(tenantId, true)).ToArray();

            var resultados = new List<SlotDisponivel>();
            var duracao = TimeSpan.FromMinutes(servico.SerDuracaoMinutos);
            var buffer = TimeSpan.FromMinutes(tenant.TenBufferMinutos);

            foreach (var rec in recursos.Where(r => r != null && r.RecAtivo))
            {
                var ocupados = (await _agendamentos.GetByPeriodoAsync(tenantId, data.Date, data.Date, rec.RecId)).ToList();

                var t = horario.HorAbertura;
                while (t.Add(duracao) <= horario.HorFechamento)
                {
                    var fim = t.Add(duracao);

                    // Pausa
                    if (horario.HorPausaInicio.HasValue && horario.HorPausaFim.HasValue
                        && t < horario.HorPausaFim && fim > horario.HorPausaInicio)
                    {
                        t = t.Add(TimeSpan.FromMinutes(15));
                        continue;
                    }

                    // Antecedência mínima
                    var dh = data.Date.Add(t);
                    if (dh < DateTime.Now.AddHours(tenant.TenAntecedenciaMinHoras))
                    {
                        t = t.Add(TimeSpan.FromMinutes(15));
                        continue;
                    }

                    // Conflito com agendamentos existentes (com buffer)
                    var conflito = ocupados.Any(a => a.AgeStatus != Core.Enums.StatusAgendamento.Cancelado
                        && a.AgeHoraInicio.Subtract(buffer) < fim
                        && a.AgeHoraFim.Add(buffer) > t);
                    if (conflito) { t = t.Add(TimeSpan.FromMinutes(15)); continue; }

                    // Bloqueio
                    var bloqueado = bloqueios.Any(b => (!b.R_RecId.HasValue || b.R_RecId == rec.RecId)
                        && b.BloDataInicio < data.Date.Add(fim) && b.BloDataFim > data.Date.Add(t));
                    if (bloqueado) { t = t.Add(TimeSpan.FromMinutes(15)); continue; }

                    resultados.Add(new SlotDisponivel
                    {
                        Data = data.Date,
                        HoraInicio = t,
                        HoraFim = fim,
                        RecursoId = rec.RecId,
                        RecursoNome = rec.RecNome
                    });
                    t = t.Add(TimeSpan.FromMinutes(15));
                }
            }

            return resultados.OrderBy(s => s.HoraInicio).ThenBy(s => s.RecursoId);
        }
    }
}

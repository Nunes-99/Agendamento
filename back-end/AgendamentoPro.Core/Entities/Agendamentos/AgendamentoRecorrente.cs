using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Agendamentos
{
    public enum FrequenciaRecorrencia { Semanal = 1, Quinzenal = 2, Mensal = 3 }

    /// <summary>
    /// Define uma série de agendamentos recorrentes (ex: toda 2ª-feira por 4 semanas).
    /// Os agendamentos individuais são criados num batch e ficam linkados via R_RecorrenciaId.
    /// </summary>
    public class AgendamentoRecorrente : ITenantScoped
    {
        public int RecId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_CliId { get; private set; }
        public int R_SerId { get; private set; }
        public int R_RecursoId { get; private set; }
        public DayOfWeek RecDiaSemana { get; private set; }
        public TimeSpan RecHoraInicio { get; private set; }
        public FrequenciaRecorrencia RecFrequencia { get; private set; }
        public int RecQuantidadeOcorrencias { get; private set; }
        public DateTime RecDataInicio { get; private set; }
        public DateTime RecCriadoEm { get; private set; }
        public bool RecAtivo { get; private set; }

        public Tenant Tenant { get; private set; }

        protected AgendamentoRecorrente() { }

        public AgendamentoRecorrente(int rTenId, int rCliId, int rSerId, int rRecId,
            DayOfWeek diaSemana, TimeSpan horaInicio, FrequenciaRecorrencia frequencia,
            int quantidade, DateTime dataInicio)
        {
            if (rTenId <= 0 || rCliId <= 0 || rSerId <= 0 || rRecId <= 0)
                throw new DomainException("Tenant, cliente, serviço e recurso obrigatórios.");
            if (quantidade < 1 || quantidade > 52)
                throw new DomainException("Quantidade deve ser entre 1 e 52.");

            R_TenId = rTenId;
            R_CliId = rCliId;
            R_SerId = rSerId;
            R_RecursoId = rRecId;
            RecDiaSemana = diaSemana;
            RecHoraInicio = horaInicio;
            RecFrequencia = frequencia;
            RecQuantidadeOcorrencias = quantidade;
            RecDataInicio = dataInicio.Date;
            RecCriadoEm = DateTime.UtcNow;
            RecAtivo = true;
        }

        public void Cancelar() => RecAtivo = false;

        /// <summary>Calcula as N datas de ocorrência da série.</summary>
        public IEnumerable<DateTime> GerarDatas()
        {
            var data = RecDataInicio;
            // Ajusta para o próximo dia da semana correto
            while (data.DayOfWeek != RecDiaSemana) data = data.AddDays(1);

            for (var i = 0; i < RecQuantidadeOcorrencias; i++)
            {
                yield return data;
                data = RecFrequencia switch
                {
                    FrequenciaRecorrencia.Semanal => data.AddDays(7),
                    FrequenciaRecorrencia.Quinzenal => data.AddDays(14),
                    FrequenciaRecorrencia.Mensal => data.AddMonths(1),
                    _ => data.AddDays(7)
                };
            }
        }
    }
}

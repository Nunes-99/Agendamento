using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;
// DomainException

namespace AgendamentoPro.Core.Entities.Horarios
{
    /// <summary>
    /// Horário de funcionamento de um tenant em determinado dia da semana.
    /// (0 = Domingo, 6 = Sábado, conforme System.DayOfWeek)
    /// </summary>
    public class HorarioFuncionamento : ITenantScoped
    {
        public int HorId { get; private set; }
        public int R_TenId { get; private set; }
        public DayOfWeek HorDiaSemana { get; private set; }
        public TimeSpan HorAbertura { get; private set; }
        public TimeSpan HorFechamento { get; private set; }
        public TimeSpan? HorPausaInicio { get; private set; }
        public TimeSpan? HorPausaFim { get; private set; }
        public bool HorAberto { get; private set; }

        public Tenant Tenant { get; private set; }

        protected HorarioFuncionamento() { }

        public HorarioFuncionamento(int rTenId, DayOfWeek diaSemana, TimeSpan abertura, TimeSpan fechamento,
            TimeSpan? pausaInicio, TimeSpan? pausaFim, bool aberto)
        {
            R_TenId = rTenId;
            HorDiaSemana = diaSemana;
            HorAbertura = abertura;
            HorFechamento = fechamento;
            HorPausaInicio = pausaInicio;
            HorPausaFim = pausaFim;
            HorAberto = aberto;
            Validate();
        }

        public void Atualizar(TimeSpan abertura, TimeSpan fechamento,
            TimeSpan? pausaInicio, TimeSpan? pausaFim, bool aberto)
        {
            HorAbertura = abertura;
            HorFechamento = fechamento;
            HorPausaInicio = pausaInicio;
            HorPausaFim = pausaFim;
            HorAberto = aberto;
            Validate();
        }

        private void Validate()
        {
            if (HorAberto && HorAbertura >= HorFechamento)
                throw new DomainException("Horário de fechamento deve ser maior que o de abertura.");
            if (HorPausaInicio.HasValue && HorPausaFim.HasValue && HorPausaInicio >= HorPausaFim)
                throw new DomainException("Pausa fim deve ser maior que pausa início.");
        }
    }
}

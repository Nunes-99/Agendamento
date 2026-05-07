using AgendamentoPro.Core.Entities.Horarios;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IHorarioFuncionamentoRepository
    {
        Task<IEnumerable<HorarioFuncionamento>> GetByTenantAsync(int tenantId);
        Task<HorarioFuncionamento> GetByDiaAsync(int tenantId, DayOfWeek dia);
        Task<int> CreateAsync(HorarioFuncionamento horario);
        Task UpdateAsync(HorarioFuncionamento horario);

        Task<IEnumerable<BloqueioAgenda>> GetBloqueiosAsync(int tenantId, DateTime inicio, DateTime fim);
        Task<int> CreateBloqueioAsync(BloqueioAgenda bloqueio);
    }
}

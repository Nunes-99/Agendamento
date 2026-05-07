using AgendamentoPro.Core.Entities.Agendamentos;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IFotoAgendamentoRepository
    {
        Task<FotoAgendamento> GetByIdAsync(int id, int tenantId);
        Task<IEnumerable<FotoAgendamento>> GetByAgendamentoAsync(int agendamentoId, int tenantId);
        Task<int> CreateAsync(FotoAgendamento foto);
        Task DeleteAsync(int id, int tenantId);
    }
}

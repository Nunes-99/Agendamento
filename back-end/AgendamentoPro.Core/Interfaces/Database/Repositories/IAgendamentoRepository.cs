using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Enums;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IAgendamentoRepository
    {
        Task<Agendamento> GetByIdAsync(int id, int tenantId);
        Task<IEnumerable<Agendamento>> GetByPeriodoAsync(int tenantId, DateTime inicio, DateTime fim, int? recursoId = null);
        Task<IEnumerable<Agendamento>> GetPorClienteAsync(int tenantId, int clienteId);
        Task<(IEnumerable<Agendamento> Items, int Total)> GetPagedAsync(int tenantId, int page, int pageSize, DateTime? data, StatusAgendamento? status);
        Task<bool> ExisteConflitoAsync(int tenantId, int recursoId, DateTime data, TimeSpan inicio, TimeSpan fim, int? ignorarAgendamentoId = null);
        Task<int> CreateAsync(Agendamento agendamento);
        Task UpdateAsync(Agendamento agendamento);
        Task<IEnumerable<Agendamento>> GetExpiradosPagamentoAsync();
        Task<IEnumerable<Agendamento>> GetByGrupoComboAsync(Guid grupoComboId);
    }
}

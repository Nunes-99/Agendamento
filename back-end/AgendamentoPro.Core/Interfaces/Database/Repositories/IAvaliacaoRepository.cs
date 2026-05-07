using AgendamentoPro.Core.Entities.Agendamentos;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IAvaliacaoRepository
    {
        Task<Avaliacao> GetByIdAsync(int id, int tenantId);
        Task<Avaliacao> GetByTokenAsync(Guid token);
        Task<Avaliacao> GetByAgendamentoAsync(int agendamentoId);
        Task<(IEnumerable<Avaliacao> Items, int Total)> GetPagedAsync(int tenantId, int page, int pageSize, bool somenteRespondidas);
        Task<IEnumerable<Avaliacao>> GetPublicasAsync(int tenantId, int top);
        Task<(decimal Media, int Total)> CalcularResumoAsync(int tenantId);
        Task<int> CreateAsync(Avaliacao avaliacao);
        Task UpdateAsync(Avaliacao avaliacao);
    }
}

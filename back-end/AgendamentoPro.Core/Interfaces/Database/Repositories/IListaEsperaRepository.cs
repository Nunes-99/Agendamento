using AgendamentoPro.Core.Entities.Agendamentos;

namespace AgendamentoPro.Core.Interfaces.Database.Repositories
{
    public interface IListaEsperaRepository
    {
        Task<ListaEspera> GetPrimeiroNaoNotificadoAsync(int tenantId, int servicoId, DateTime data);
        Task UpdateAsync(ListaEspera item);
    }
}

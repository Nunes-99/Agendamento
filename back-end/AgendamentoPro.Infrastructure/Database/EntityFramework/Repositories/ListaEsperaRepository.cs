using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class ListaEsperaRepository : IListaEsperaRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public ListaEsperaRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<ListaEspera> GetPrimeiroNaoNotificadoAsync(int tenantId, int servicoId, DateTime data)
            => _ctx.ListaEspera
                .Where(l => l.R_TenId == tenantId
                    && l.R_SerId == servicoId
                    && l.LesDataDesejada == data.Date
                    && !l.LesNotificado)
                .OrderBy(l => l.LesCriadoEm)
                .FirstOrDefaultAsync();

        public Task UpdateAsync(ListaEspera item)
        {
            _ctx.ListaEspera.Update(item);
            return Task.CompletedTask;
        }
    }
}

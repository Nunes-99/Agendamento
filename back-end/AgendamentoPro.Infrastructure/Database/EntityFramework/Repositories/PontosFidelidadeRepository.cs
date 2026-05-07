using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class PontosFidelidadeRepository : IPontosFidelidadeRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public PontosFidelidadeRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<PontosFidelidade> GetAsync(int tenantId, int clienteId)
            => _ctx.PontosFidelidade.FirstOrDefaultAsync(
                p => p.R_TenId == tenantId && p.R_CliId == clienteId);

        public async Task<int> CreateAsync(PontosFidelidade pontos)
        {
            _ctx.PontosFidelidade.Add(pontos);
            await _ctx.SaveChangesAsync();
            return pontos.PtsId;
        }

        public Task UpdateAsync(PontosFidelidade pontos)
        {
            _ctx.PontosFidelidade.Update(pontos);
            return Task.CompletedTask;
        }
    }
}

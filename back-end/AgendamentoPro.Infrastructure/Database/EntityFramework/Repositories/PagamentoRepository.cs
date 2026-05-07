using AgendamentoPro.Core.Entities.Pagamentos;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class PagamentoRepository : IPagamentoRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public PagamentoRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Pagamento> GetByIdAsync(int id)
            => _ctx.Pagamentos.FirstOrDefaultAsync(p => p.PagId == id);

        public Task<Pagamento> GetByGatewayIdAsync(string gatewayId)
            => _ctx.Pagamentos.FirstOrDefaultAsync(p => p.PagGatewayId == gatewayId);

        public async Task<IEnumerable<Pagamento>> GetByAgendamentoAsync(int agendamentoId)
            => await _ctx.Pagamentos.AsNoTracking().Where(p => p.R_AgeId == agendamentoId).ToListAsync();

        public async Task<int> CreateAsync(Pagamento pagamento)
        {
            _ctx.Pagamentos.Add(pagamento);
            await _ctx.SaveChangesAsync();
            return pagamento.PagId;
        }

        public Task UpdateAsync(Pagamento pagamento)
        {
            _ctx.Pagamentos.Update(pagamento);
            return Task.CompletedTask;
        }
    }
}

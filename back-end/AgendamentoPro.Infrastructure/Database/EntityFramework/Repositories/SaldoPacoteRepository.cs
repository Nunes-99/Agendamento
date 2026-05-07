using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class SaldoPacoteRepository : ISaldoPacoteRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public SaldoPacoteRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<SaldoPacote> GetSaldoValidoAsync(int tenantId, int clienteId, int servicoId)
        {
            var agora = DateTime.UtcNow;
            return _ctx.SaldosPacote
                .Include(s => s.Pacote)
                .Where(s => s.R_TenId == tenantId
                    && s.R_CliId == clienteId
                    && s.SaldQuantidadeRestante > 0
                    && s.SaldExpiraEm > agora
                    && s.Pacote.R_SerId == servicoId)
                .OrderBy(s => s.SaldExpiraEm) // usa primeiro o que expira mais cedo
                .FirstOrDefaultAsync();
        }

        public Task<SaldoPacote> GetByGatewayIdAsync(string gatewayId)
            => _ctx.SaldosPacote.FirstOrDefaultAsync(s => s.SaldGatewayPagamentoId == gatewayId);

        public async Task<int> CreateAsync(SaldoPacote saldo)
        {
            _ctx.SaldosPacote.Add(saldo);
            await _ctx.SaveChangesAsync();
            return saldo.SaldId;
        }

        public Task UpdateAsync(SaldoPacote saldo)
        {
            _ctx.SaldosPacote.Update(saldo);
            return Task.CompletedTask;
        }
    }
}

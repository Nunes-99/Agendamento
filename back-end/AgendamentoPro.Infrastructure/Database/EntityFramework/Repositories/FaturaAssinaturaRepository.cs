using AgendamentoPro.Core.Entities.Assinaturas;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class FaturaAssinaturaRepository : IFaturaAssinaturaRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public FaturaAssinaturaRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<FaturaAssinatura> GetByIdAsync(int id)
            => _ctx.FaturasAssinatura.FirstOrDefaultAsync(f => f.FasId == id);

        public Task<FaturaAssinatura> GetByGatewayPaymentIdAsync(string gatewayPaymentId)
            => _ctx.FaturasAssinatura.FirstOrDefaultAsync(f => f.FasGatewayPaymentId == gatewayPaymentId);

        public async Task<IEnumerable<FaturaAssinatura>> ListarPorAssinaturaAsync(int assinaturaId)
            => await _ctx.FaturasAssinatura.AsNoTracking()
                .Where(f => f.R_AssId == assinaturaId)
                .OrderByDescending(f => f.FasReferenciaInicio)
                .ToListAsync();

        public async Task<int> CreateAsync(FaturaAssinatura fatura)
        {
            _ctx.FaturasAssinatura.Add(fatura);
            await _ctx.SaveChangesAsync();
            return fatura.FasId;
        }

        public Task UpdateAsync(FaturaAssinatura fatura)
        {
            _ctx.FaturasAssinatura.Update(fatura);
            return Task.CompletedTask;
        }
    }
}

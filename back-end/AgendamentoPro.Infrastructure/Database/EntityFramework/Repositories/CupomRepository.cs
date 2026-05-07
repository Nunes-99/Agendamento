using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class CupomRepository : ICupomRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public CupomRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Cupom> GetByCodigoAsync(int tenantId, string codigo)
            => _ctx.Cupons.FirstOrDefaultAsync(c => c.R_TenId == tenantId
                && c.CupCodigo == codigo.Trim().ToUpper() && !c.Excluido);

        public Task UpdateAsync(Cupom cupom)
        {
            _ctx.Cupons.Update(cupom);
            return Task.CompletedTask;
        }
    }
}

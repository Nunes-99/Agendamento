using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class ComboRepository : IComboRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public ComboRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Combo> GetByIdAsync(int id, int tenantId)
            => _ctx.Combos
                .Include(c => c.Servicos).ThenInclude(cs => cs.Servico)
                .FirstOrDefaultAsync(c => c.ComId == id && c.R_TenId == tenantId && !c.Excluido);

        public async Task<IEnumerable<Combo>> GetByTenantAsync(int tenantId, bool somenteAtivos)
        {
            var q = _ctx.Combos.AsNoTracking()
                .Include(c => c.Servicos).ThenInclude(cs => cs.Servico)
                .Where(c => c.R_TenId == tenantId && !c.Excluido);
            if (somenteAtivos) q = q.Where(c => c.ComAtivo);
            return await q.OrderBy(c => c.ComOrdem).ThenBy(c => c.ComNome).ToListAsync();
        }

        public async Task<int> CreateAsync(Combo combo)
        {
            _ctx.Combos.Add(combo);
            await _ctx.SaveChangesAsync();
            return combo.ComId;
        }

        public Task UpdateAsync(Combo combo)
        {
            _ctx.Combos.Update(combo);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id, int tenantId)
        {
            var c = await GetByIdAsync(id, tenantId);
            if (c != null) { c.Excluir(); _ctx.Combos.Update(c); }
        }
    }
}

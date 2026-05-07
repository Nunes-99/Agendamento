using AgendamentoPro.Core.Entities.Recursos;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class RecursoRepository : IRecursoRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public RecursoRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Recurso> GetByIdAsync(int id, int tenantId)
            => _ctx.Recursos.FirstOrDefaultAsync(r => r.RecId == id && r.R_TenId == tenantId && !r.Excluido);

        public async Task<IEnumerable<Recurso>> GetByTenantAsync(int tenantId, bool somenteAtivos)
        {
            var q = _ctx.Recursos.AsNoTracking().Where(r => r.R_TenId == tenantId && !r.Excluido);
            if (somenteAtivos) q = q.Where(r => r.RecAtivo);
            return await q.OrderBy(r => r.RecOrdem).ThenBy(r => r.RecNome).ToListAsync();
        }

        public async Task<int> CreateAsync(Recurso recurso)
        {
            _ctx.Recursos.Add(recurso);
            await _ctx.SaveChangesAsync();
            return recurso.RecId;
        }

        public Task UpdateAsync(Recurso recurso)
        {
            _ctx.Recursos.Update(recurso);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id, int tenantId)
        {
            var r = await GetByIdAsync(id, tenantId);
            if (r != null) { r.Excluir(); _ctx.Recursos.Update(r); }
        }
    }
}

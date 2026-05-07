using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class ServicoRepository : IServicoRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public ServicoRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Servico> GetByIdAsync(int id, int tenantId)
            => _ctx.Servicos.FirstOrDefaultAsync(s => s.SerId == id && s.R_TenId == tenantId && !s.Excluido);

        public async Task<IEnumerable<Servico>> GetByTenantAsync(int tenantId, bool somenteAtivos)
        {
            var q = _ctx.Servicos.AsNoTracking().Where(s => s.R_TenId == tenantId && !s.Excluido);
            if (somenteAtivos) q = q.Where(s => s.SerAtivo);
            return await q.OrderBy(s => s.SerOrdem).ThenBy(s => s.SerNome).ToListAsync();
        }

        public async Task<int> CreateAsync(Servico servico)
        {
            _ctx.Servicos.Add(servico);
            await _ctx.SaveChangesAsync();
            return servico.SerId;
        }

        public Task UpdateAsync(Servico servico)
        {
            _ctx.Servicos.Update(servico);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id, int tenantId)
        {
            var s = await GetByIdAsync(id, tenantId);
            if (s != null) { s.Excluir(); _ctx.Servicos.Update(s); }
        }
    }
}

using AgendamentoPro.Core.Entities.Assinaturas;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class PlanoRepository : IPlanoRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public PlanoRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Plano> GetByIdAsync(int id)
            => _ctx.Planos.FirstOrDefaultAsync(p => p.PlnId == id);

        public async Task<IEnumerable<Plano>> ListarPublicosAsync()
            => await _ctx.Planos.AsNoTracking()
                .Where(p => p.PlnAtivo && p.PlnPublico)
                .OrderBy(p => p.PlnOrdem).ThenBy(p => p.PlnPreco)
                .ToListAsync();

        public async Task<IEnumerable<Plano>> ListarTodosAsync()
            => await _ctx.Planos.AsNoTracking()
                .OrderBy(p => p.PlnOrdem).ThenBy(p => p.PlnPreco)
                .ToListAsync();

        public async Task<int> CreateAsync(Plano plano)
        {
            _ctx.Planos.Add(plano);
            await _ctx.SaveChangesAsync();
            return plano.PlnId;
        }

        public Task UpdateAsync(Plano plano)
        {
            _ctx.Planos.Update(plano);
            return Task.CompletedTask;
        }
    }
}

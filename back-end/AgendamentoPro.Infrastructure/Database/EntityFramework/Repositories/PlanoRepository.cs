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

        // O desempate por preço é feito EM MEMÓRIA de propósito: o SQLite — que é
        // o provider padrão — não ordena por decimal e devolve 500 se a expressão
        // for traduzida para SQL. São dois ou três planos no catálogo; ordenar
        // essa lista na aplicação não custa nada e vale para qualquer provider.
        public async Task<IEnumerable<Plano>> ListarPublicosAsync()
            => (await _ctx.Planos.AsNoTracking()
                .Where(p => p.PlnAtivo && p.PlnPublico)
                .OrderBy(p => p.PlnOrdem)
                .ToListAsync())
                .OrderBy(p => p.PlnOrdem).ThenBy(p => p.PlnPreco)
                .ToList();

        public async Task<IEnumerable<Plano>> ListarTodosAsync()
            => (await _ctx.Planos.AsNoTracking()
                .OrderBy(p => p.PlnOrdem)
                .ToListAsync())
                .OrderBy(p => p.PlnOrdem).ThenBy(p => p.PlnPreco)
                .ToList();

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

using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class AvaliacaoRepository : IAvaliacaoRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public AvaliacaoRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Avaliacao> GetByIdAsync(int id, int tenantId)
            => _ctx.Avaliacoes.FirstOrDefaultAsync(a => a.AvaId == id && a.R_TenId == tenantId);

        public Task<Avaliacao> GetByTokenAsync(Guid token)
            => _ctx.Avaliacoes.FirstOrDefaultAsync(a => a.AvaToken == token);

        public Task<Avaliacao> GetByAgendamentoAsync(int agendamentoId)
            => _ctx.Avaliacoes.FirstOrDefaultAsync(a => a.R_AgeId == agendamentoId);

        public async Task<(IEnumerable<Avaliacao> Items, int Total)> GetPagedAsync(int tenantId, int page, int pageSize, bool somenteRespondidas)
        {
            var q = _ctx.Avaliacoes.AsNoTracking()
                .Include(a => a.Cliente)
                .Where(a => a.R_TenId == tenantId);
            if (somenteRespondidas)
                q = q.Where(a => a.AvaRespondidoEm != null);
            var total = await q.CountAsync();
            var items = await q.OrderByDescending(a => a.AvaCriadoEm)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        public async Task<IEnumerable<Avaliacao>> GetPublicasAsync(int tenantId, int top)
            => await _ctx.Avaliacoes.AsNoTracking()
                .Include(a => a.Cliente)
                .Where(a => a.R_TenId == tenantId && a.AvaPublica && a.AvaRespondidoEm != null && a.AvaNota != null)
                .OrderByDescending(a => a.AvaRespondidoEm)
                .Take(top)
                .ToListAsync();

        public async Task<(decimal Media, int Total)> CalcularResumoAsync(int tenantId)
        {
            var lista = await _ctx.Avaliacoes.AsNoTracking()
                .Where(a => a.R_TenId == tenantId && a.AvaNota != null)
                .Select(a => a.AvaNota!.Value)
                .ToListAsync();
            if (lista.Count == 0) return (0m, 0);
            return ((decimal)lista.Average(), lista.Count);
        }

        public async Task<int> CreateAsync(Avaliacao avaliacao)
        {
            _ctx.Avaliacoes.Add(avaliacao);
            await _ctx.SaveChangesAsync();
            return avaliacao.AvaId;
        }

        public Task UpdateAsync(Avaliacao avaliacao)
        {
            _ctx.Avaliacoes.Update(avaliacao);
            return Task.CompletedTask;
        }
    }
}

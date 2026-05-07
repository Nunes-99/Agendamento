using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class FotoAgendamentoRepository : IFotoAgendamentoRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public FotoAgendamentoRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<FotoAgendamento> GetByIdAsync(int id, int tenantId)
            => _ctx.FotosAgendamento
                .FirstOrDefaultAsync(f => f.FotId == id && f.R_TenId == tenantId);

        public async Task<IEnumerable<FotoAgendamento>> GetByAgendamentoAsync(int agendamentoId, int tenantId)
            => await _ctx.FotosAgendamento.AsNoTracking()
                .Where(f => f.R_AgeId == agendamentoId && f.R_TenId == tenantId)
                .OrderBy(f => f.FotCriadoEm)
                .ToListAsync();

        public async Task<int> CreateAsync(FotoAgendamento foto)
        {
            _ctx.FotosAgendamento.Add(foto);
            await _ctx.SaveChangesAsync();
            return foto.FotId;
        }

        public async Task DeleteAsync(int id, int tenantId)
        {
            var f = await GetByIdAsync(id, tenantId);
            if (f != null) _ctx.FotosAgendamento.Remove(f);
        }
    }
}

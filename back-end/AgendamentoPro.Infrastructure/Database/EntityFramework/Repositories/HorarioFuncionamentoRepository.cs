using AgendamentoPro.Core.Entities.Horarios;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class HorarioFuncionamentoRepository : IHorarioFuncionamentoRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public HorarioFuncionamentoRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public async Task<IEnumerable<HorarioFuncionamento>> GetByTenantAsync(int tenantId)
            => await _ctx.HorariosFuncionamento.AsNoTracking().Where(h => h.R_TenId == tenantId)
                .OrderBy(h => h.HorDiaSemana).ToListAsync();

        public Task<HorarioFuncionamento> GetByDiaAsync(int tenantId, DayOfWeek dia)
            => _ctx.HorariosFuncionamento.FirstOrDefaultAsync(h => h.R_TenId == tenantId && h.HorDiaSemana == dia);

        public async Task<int> CreateAsync(HorarioFuncionamento horario)
        {
            _ctx.HorariosFuncionamento.Add(horario);
            await _ctx.SaveChangesAsync();
            return horario.HorId;
        }

        public Task UpdateAsync(HorarioFuncionamento horario)
        {
            _ctx.HorariosFuncionamento.Update(horario);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<BloqueioAgenda>> GetBloqueiosAsync(int tenantId, DateTime inicio, DateTime fim)
            => await _ctx.BloqueiosAgenda
                .AsNoTracking()
                .Where(b => b.R_TenId == tenantId && b.BloDataFim >= inicio && b.BloDataInicio <= fim)
                .ToListAsync();

        public async Task<int> CreateBloqueioAsync(BloqueioAgenda bloqueio)
        {
            _ctx.BloqueiosAgenda.Add(bloqueio);
            await _ctx.SaveChangesAsync();
            return bloqueio.BloId;
        }
    }
}

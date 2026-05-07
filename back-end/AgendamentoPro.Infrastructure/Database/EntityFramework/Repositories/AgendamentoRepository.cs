using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class AgendamentoRepository : IAgendamentoRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public AgendamentoRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Agendamento> GetByIdAsync(int id, int tenantId)
            => _ctx.Agendamentos
                .Include(a => a.Cliente)
                .Include(a => a.Servico)
                .Include(a => a.Recurso)
                .Include(a => a.Pagamentos)
                .FirstOrDefaultAsync(a => a.AgeId == id && a.R_TenId == tenantId);

        public async Task<IEnumerable<Agendamento>> GetByPeriodoAsync(int tenantId, DateTime inicio, DateTime fim, int? recursoId = null)
        {
            var q = _ctx.Agendamentos
                .AsNoTracking()
                .Include(a => a.Cliente)
                .Include(a => a.Servico)
                .Include(a => a.Recurso)
                .Where(a => a.R_TenId == tenantId && a.AgeData >= inicio.Date && a.AgeData <= fim.Date);
            if (recursoId.HasValue) q = q.Where(a => a.R_RecId == recursoId.Value);
            // ORDER BY por TimeSpan não é suportado pelo SQLite — ordena em memória.
            var lista = await q.OrderBy(a => a.AgeData).ToListAsync();
            return lista.OrderBy(a => a.AgeData).ThenBy(a => a.AgeHoraInicio);
        }

        public async Task<IEnumerable<Agendamento>> GetPorClienteAsync(int tenantId, int clienteId)
            => await _ctx.Agendamentos
                .AsNoTracking()
                .Include(a => a.Servico)
                .Where(a => a.R_TenId == tenantId && a.R_CliId == clienteId)
                .OrderByDescending(a => a.AgeData)
                .ToListAsync();

        public async Task<(IEnumerable<Agendamento> Items, int Total)> GetPagedAsync(int tenantId, int page, int pageSize, DateTime? data, StatusAgendamento? status)
        {
            var q = _ctx.Agendamentos
                .AsNoTracking()
                .Include(a => a.Cliente)
                .Include(a => a.Servico)
                .Include(a => a.Recurso)
                .Where(a => a.R_TenId == tenantId);
            if (data.HasValue) q = q.Where(a => a.AgeData == data.Value.Date);
            if (status.HasValue) q = q.Where(a => a.AgeStatus == status.Value);

            var total = await q.CountAsync();
            // ORDER BY por TimeSpan não traduz no SQLite — busca por data e refina em memória.
            var pagina = await q.OrderByDescending(a => a.AgeData)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var items = pagina.OrderByDescending(a => a.AgeData).ThenBy(a => a.AgeHoraInicio);
            return (items, total);
        }

        public async Task<bool> ExisteConflitoAsync(int tenantId, int recursoId, DateTime data, TimeSpan inicio, TimeSpan fim, int? ignorarAgendamentoId = null)
        {
            var dataLimpa = data.Date;
            // Filtra no banco pelos campos traduzíveis (data, recurso, status) e
            // faz overlap de horários em memória (TimeSpan não traduz no SQLite).
            var candidatos = await _ctx.Agendamentos
                .Where(a => a.R_TenId == tenantId
                    && a.R_RecId == recursoId
                    && a.AgeData == dataLimpa
                    && a.AgeStatus != StatusAgendamento.Cancelado)
                .Select(a => new { a.AgeId, a.AgeHoraInicio, a.AgeHoraFim })
                .ToListAsync();

            return candidatos.Any(c =>
                (!ignorarAgendamentoId.HasValue || c.AgeId != ignorarAgendamentoId.Value)
                && c.AgeHoraInicio < fim
                && c.AgeHoraFim > inicio);
        }

        public async Task<int> CreateAsync(Agendamento agendamento)
        {
            _ctx.Agendamentos.Add(agendamento);
            await _ctx.SaveChangesAsync();
            return agendamento.AgeId;
        }

        public Task UpdateAsync(Agendamento agendamento)
        {
            _ctx.Agendamentos.Update(agendamento);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<Agendamento>> GetExpiradosPagamentoAsync()
        {
            var agora = DateTime.UtcNow;
            return await _ctx.Agendamentos
                .Include(a => a.Pagamentos)
                .Where(a => a.AgePagamentoStatus == StatusPagamento.Pendente
                    && a.Pagamentos.Any(p => p.PagExpiracao < agora && p.PagStatus == StatusPagamento.Pendente))
                .ToListAsync();
        }
    }
}

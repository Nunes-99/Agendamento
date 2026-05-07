using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly AgendamentoProDbContext _ctx;
        public ClienteRepository(AgendamentoProDbContext ctx) { _ctx = ctx; }

        public Task<Cliente> GetByIdAsync(int id, int tenantId)
            => _ctx.Clientes.FirstOrDefaultAsync(c => c.CliId == id && c.R_TenId == tenantId && !c.Excluido);

        public Task<Cliente> GetByTelefoneAsync(int tenantId, string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone)) return Task.FromResult<Cliente>(null);
            return _ctx.Clientes.FirstOrDefaultAsync(c => c.R_TenId == tenantId
                && (c.CliTelefone == telefone || c.CliWhatsApp == telefone) && !c.Excluido);
        }

        public Task<Cliente> GetByEmailAsync(int tenantId, string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return Task.FromResult<Cliente>(null);
            return _ctx.Clientes.FirstOrDefaultAsync(c => c.R_TenId == tenantId
                && c.CliEmail == email && !c.Excluido);
        }

        public async Task<(IEnumerable<Cliente> Items, int Total)> GetPagedAsync(int tenantId, int page, int pageSize, string busca)
        {
            var q = _ctx.Clientes.AsNoTracking().Where(c => c.R_TenId == tenantId && !c.Excluido);
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var b = busca.ToLower();
                q = q.Where(c => c.CliNome.ToLower().Contains(b)
                    || (c.CliEmail != null && c.CliEmail.ToLower().Contains(b))
                    || (c.CliTelefone != null && c.CliTelefone.Contains(b))
                    || (c.CliWhatsApp != null && c.CliWhatsApp.Contains(b)));
            }
            var total = await q.CountAsync();
            var items = await q.OrderBy(c => c.CliNome)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        public async Task<int> CreateAsync(Cliente cliente)
        {
            _ctx.Clientes.Add(cliente);
            await _ctx.SaveChangesAsync();
            return cliente.CliId;
        }

        public Task UpdateAsync(Cliente cliente)
        {
            _ctx.Clientes.Update(cliente);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id, int tenantId)
        {
            var c = await GetByIdAsync(id, tenantId);
            if (c != null) { c.Excluir(); _ctx.Clientes.Update(c); }
        }

        public async Task<IEnumerable<int>> GetIdsInativosAsync(int tenantId, DateTime corte)
        {
            // Single query: clientes do tenant que NÃO têm nenhum agendamento ativo
            // (não cancelado) com AgeData >= corte. Inclui clientes que nunca agendaram
            // e foram criados antes do corte.
            // O StartsWith filtra clientes já anonimizados (nome "Cliente removido #N").
            return await _ctx.Clientes.AsNoTracking()
                .Where(c => c.R_TenId == tenantId
                    && !c.Excluido
                    && !c.CliNome.StartsWith("Cliente removido")
                    && c.CliCriadoEm < corte
                    && !_ctx.Agendamentos.Any(a =>
                        a.R_CliId == c.CliId
                        && a.AgeData >= corte
                        && a.AgeStatus != Core.Enums.StatusAgendamento.Cancelado))
                .Select(c => c.CliId)
                .ToListAsync();
        }
    }
}

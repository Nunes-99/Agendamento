using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Tests.Database
{
    /// <summary>
    /// Soft Delete via QueryFilter global: linhas com Excluido=true não aparecem
    /// em queries normais. Para vê-las (ex: telas de auditoria), usa-se
    /// .IgnoreQueryFilters().
    /// </summary>
    public class SoftDeleteFilterTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly DbContextOptions<AgendamentoProDbContext> _opts;
        private readonly TestDb.SeedIds _ids;

        public SoftDeleteFilterTests()
        {
            (_conn, _opts) = TestDb.Create();
            _ids = TestDb.Seed(_opts);
        }

        public void Dispose() => _conn.Dispose();

        [Fact]
        public async Task ServicoExcluido_NaoAparecePorPadrao()
        {
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var s = new Servico(_ids.TenantId, "Lavagem extra", null, 50m, 30, null, null, 0);
                ctx.Servicos.Add(s);
                await ctx.SaveChangesAsync();
                s.Excluir();
                ctx.Servicos.Update(s);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var ativos = await ctx.Servicos.Where(s => s.SerNome == "Lavagem extra").ToListAsync();
                ativos.Should().BeEmpty("filter global deve esconder soft-deleted");
            }
        }

        [Fact]
        public async Task IgnoreQueryFilters_RecuperaSoftDeleted()
        {
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var s = new Servico(_ids.TenantId, "Lavagem extra", null, 50m, 30, null, null, 0);
                ctx.Servicos.Add(s);
                await ctx.SaveChangesAsync();
                s.Excluir();
                ctx.Servicos.Update(s);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var todos = await ctx.Servicos.IgnoreQueryFilters()
                    .Where(s => s.SerNome == "Lavagem extra").ToListAsync();
                todos.Should().HaveCount(1);
                todos[0].Excluido.Should().BeTrue();
            }
        }
    }
}

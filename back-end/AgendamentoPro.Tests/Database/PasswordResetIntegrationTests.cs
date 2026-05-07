using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Tests.Database
{
    /// <summary>
    /// O índice único em RpsToken impede que dois resets com mesmo token coexistam
    /// (defesa em profundidade contra colisão de Guid em ambiente comprometido).
    /// </summary>
    public class PasswordResetIntegrationTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly DbContextOptions<AgendamentoProDbContext> _opts;

        public PasswordResetIntegrationTests()
        {
            _conn = new SqliteConnection("DataSource=:memory:");
            _conn.Open();
            _opts = new DbContextOptionsBuilder<AgendamentoProDbContext>()
                .UseSqlite(_conn)
                .Options;
            using var ctx = new AgendamentoProDbContext(_opts);
            ctx.Database.EnsureCreated();
        }

        public void Dispose() => _conn.Dispose();

        private async Task<int> SeedUsuarioAsync()
        {
            using var ctx = new AgendamentoProDbContext(_opts);
            var u = new Usuario(null, "Admin", "admin@x.com", "hash", PerfilUsuario.SuperAdmin, null);
            ctx.Usuarios.Add(u);
            await ctx.SaveChangesAsync();
            return u.UsuId;
        }

        [Fact]
        public async Task DoisResetsMesmoToken_SegundoFalha()
        {
            var usuId = await SeedUsuarioAsync();
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                ctx.PasswordResets.Add(new PasswordReset(usuId, "duplicado", TimeSpan.FromHours(1)));
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                ctx.PasswordResets.Add(new PasswordReset(usuId, "duplicado", TimeSpan.FromHours(1)));
                Func<Task> act = async () => await ctx.SaveChangesAsync();
                await act.Should().ThrowAsync<ConcorrenciaException>();
            }
        }

        [Fact]
        public async Task InvalidarPendentes_MarcaTodosComoUsados()
        {
            var usuId = await SeedUsuarioAsync();
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                ctx.PasswordResets.Add(new PasswordReset(usuId, "tk-1", TimeSpan.FromHours(1)));
                ctx.PasswordResets.Add(new PasswordReset(usuId, "tk-2", TimeSpan.FromHours(1)));
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var repo = new PasswordResetRepository(ctx);
                await repo.InvalidarPendentesAsync(usuId);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var todos = await ctx.PasswordResets.Where(r => r.R_UsuId == usuId).ToListAsync();
                todos.Should().AllSatisfy(r => r.RpsUsado.Should().BeTrue());
            }
        }
    }
}

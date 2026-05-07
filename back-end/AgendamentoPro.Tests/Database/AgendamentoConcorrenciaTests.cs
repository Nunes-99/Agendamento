using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Tests.Database
{
    /// <summary>
    /// O índice único (R_RecId, AgeData, AgeHoraInicio) deve impedir dois agendamentos
    /// concorrentes para o mesmo recurso/data/horário. Esses testes usam SQLite in-memory
    /// para validar que o DbContext converte o erro em ConcorrenciaException.
    /// </summary>
    public class AgendamentoConcorrenciaTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly DbContextOptions<AgendamentoProDbContext> _opts;
        private readonly TestDb.SeedIds _ids;

        public AgendamentoConcorrenciaTests()
        {
            (_conn, _opts) = TestDb.Create();
            _ids = TestDb.Seed(_opts);
        }

        public void Dispose() => _conn.Dispose();

        [Fact]
        public async Task DoisAgendamentosMesmoRecursoDataHora_SegundoDeveFalhar()
        {
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var ag1 = new Agendamento(_ids.TenantId, _ids.ClienteAId, _ids.ServicoId, _ids.RecursoAId,
                    DateTime.Today.AddDays(3), TimeSpan.FromHours(10), TimeSpan.FromHours(11),
                    100m, 20m, null);
                ctx.Agendamentos.Add(ag1);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var ag2 = new Agendamento(_ids.TenantId, _ids.ClienteBId, _ids.ServicoId, _ids.RecursoAId,
                    DateTime.Today.AddDays(3), TimeSpan.FromHours(10), TimeSpan.FromHours(11),
                    100m, 20m, null);
                ctx.Agendamentos.Add(ag2);

                Func<Task> act = async () => await ctx.SaveChangesAsync();
                await act.Should().ThrowAsync<ConcorrenciaException>();
            }
        }

        [Fact]
        public async Task DoisAgendamentosMesmoRecursoDatasDiferentes_AmbosDevemPersistir()
        {
            using var ctx = new AgendamentoProDbContext(_opts);

            var ag1 = new Agendamento(_ids.TenantId, _ids.ClienteAId, _ids.ServicoId, _ids.RecursoAId,
                DateTime.Today.AddDays(1), TimeSpan.FromHours(10), TimeSpan.FromHours(11),
                100m, 20m, null);
            var ag2 = new Agendamento(_ids.TenantId, _ids.ClienteBId, _ids.ServicoId, _ids.RecursoAId,
                DateTime.Today.AddDays(2), TimeSpan.FromHours(10), TimeSpan.FromHours(11),
                100m, 20m, null);

            ctx.Agendamentos.AddRange(ag1, ag2);
            await ctx.SaveChangesAsync();

            (await ctx.Agendamentos.CountAsync()).Should().Be(2);
        }

        [Fact]
        public async Task DoisAgendamentosRecursosDiferentesMesmoHorario_AmbosDevemPersistir()
        {
            using var ctx = new AgendamentoProDbContext(_opts);

            var ag1 = new Agendamento(_ids.TenantId, _ids.ClienteAId, _ids.ServicoId, _ids.RecursoAId,
                DateTime.Today.AddDays(1), TimeSpan.FromHours(10), TimeSpan.FromHours(11),
                100m, 20m, null);
            var ag2 = new Agendamento(_ids.TenantId, _ids.ClienteBId, _ids.ServicoId, _ids.RecursoBId,
                DateTime.Today.AddDays(1), TimeSpan.FromHours(10), TimeSpan.FromHours(11),
                100m, 20m, null);

            ctx.Agendamentos.AddRange(ag1, ag2);
            await ctx.SaveChangesAsync();

            (await ctx.Agendamentos.CountAsync()).Should().Be(2);
        }
    }
}

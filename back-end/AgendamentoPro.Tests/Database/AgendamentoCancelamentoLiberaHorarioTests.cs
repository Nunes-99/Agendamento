using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Tests.Database
{
    /// <summary>
    /// Cancelar um agendamento tem que LIBERAR o horário.
    ///
    /// É a regra que o próprio código já aplica: `ExisteConflitoAsync` filtra
    /// `AgeStatus != Cancelado` — ou seja, para o sistema, horário cancelado está
    /// livre. Mas o índice único do banco é `(R_RecId, AgeData, AgeHoraInicio)`
    /// com `HasFilter(null)`, isto é, vale para TODAS as linhas, canceladas
    /// inclusive.
    ///
    /// Com isso, a checagem prévia libera o horário e o INSERT é rejeitado pelo
    /// banco. Na prática: o cliente desmarca as 14h de sábado no Box 1, e aquele
    /// horário nunca mais pode ser vendido.
    /// </summary>
    public class AgendamentoCancelamentoLiberaHorarioTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly DbContextOptions<AgendamentoProDbContext> _opts;
        private readonly TestDb.SeedIds _ids;

        public AgendamentoCancelamentoLiberaHorarioTests()
        {
            (_conn, _opts) = TestDb.Create();
            _ids = TestDb.Seed(_opts);
        }

        public void Dispose() => _conn.Dispose();

        private Agendamento Novo(int clienteId) =>
            new(
                _ids.TenantId,
                clienteId,
                _ids.ServicoId,
                _ids.RecursoAId,
                DateTime.Today.AddDays(3),
                TimeSpan.FromHours(14),
                TimeSpan.FromHours(15),
                100m,
                20m,
                null
            );

        [Fact]
        public async Task HorarioCancelado_PodeSerVendidoDeNovo()
        {
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var primeiro = Novo(_ids.ClienteAId);
                primeiro.ConfirmarPagamento();
                ctx.Agendamentos.Add(primeiro);
                await ctx.SaveChangesAsync();

                primeiro.Cancelar("Cliente desistiu");
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                ctx.Agendamentos.Add(Novo(_ids.ClienteBId));

                Func<Task> revender = async () => await ctx.SaveChangesAsync();
                await revender.Should().NotThrowAsync();
            }
        }

        [Fact]
        public async Task DoisCancelamentosNoMesmoHorario_NaoBrigamEntreSi()
        {
            // O caso que derruba a criação de tenant: o seeder de dados-exemplo
            // cancela parte dos agendamentos e depois sorteia o mesmo horário.
            using var ctx = new AgendamentoProDbContext(_opts);

            var a = Novo(_ids.ClienteAId);
            a.ConfirmarPagamento();
            ctx.Agendamentos.Add(a);
            await ctx.SaveChangesAsync();
            a.Cancelar("primeiro");
            await ctx.SaveChangesAsync();

            var b = Novo(_ids.ClienteBId);
            b.ConfirmarPagamento();
            ctx.Agendamentos.Add(b);
            await ctx.SaveChangesAsync();
            b.Cancelar("segundo");

            Func<Task> segundoCancelamento = async () => await ctx.SaveChangesAsync();
            await segundoCancelamento.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DoisAtivosNoMesmoHorario_ContinuamSendoBarrados()
        {
            // A proteção original não pode ser perdida na correção.
            using var ctx = new AgendamentoProDbContext(_opts);

            ctx.Agendamentos.Add(Novo(_ids.ClienteAId));
            await ctx.SaveChangesAsync();

            ctx.Agendamentos.Add(Novo(_ids.ClienteBId));
            Func<Task> segundo = async () => await ctx.SaveChangesAsync();
            await segundo
                .Should()
                .ThrowAsync<Core.Exceptions.ConcorrenciaException>();
        }
    }
}

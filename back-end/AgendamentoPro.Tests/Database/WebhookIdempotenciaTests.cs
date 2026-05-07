using AgendamentoPro.Core.Entities.Pagamentos;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Tests.Database
{
    /// <summary>
    /// O unique-index em (WhEvGateway, WhEvEventoId) é a barreira final de idempotência:
    /// dois webhooks com mesmo identificador devem ser rejeitados pelo banco como
    /// ConcorrenciaException.
    /// </summary>
    public class WebhookIdempotenciaTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly DbContextOptions<AgendamentoProDbContext> _opts;

        public WebhookIdempotenciaTests()
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

        [Fact]
        public async Task DoisWebhooksMesmoEventoId_SegundoDeveFalhar()
        {
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                ctx.WebhookEventos.Add(new WebhookEvento("MercadoPago", "evt-42", "payment.updated", "{}"));
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                ctx.WebhookEventos.Add(new WebhookEvento("MercadoPago", "evt-42", "payment.updated", "{}"));
                Func<Task> act = async () => await ctx.SaveChangesAsync();
                await act.Should().ThrowAsync<ConcorrenciaException>();
            }
        }

        [Fact]
        public async Task GatewaysDiferentesMesmoEventoId_AmbosDevemPersistir()
        {
            using var ctx = new AgendamentoProDbContext(_opts);
            ctx.WebhookEventos.Add(new WebhookEvento("MercadoPago", "evt-42", "x", "{}"));
            ctx.WebhookEventos.Add(new WebhookEvento("Stripe", "evt-42", "x", "{}"));
            await ctx.SaveChangesAsync();

            (await ctx.WebhookEventos.CountAsync()).Should().Be(2);
        }

        [Fact]
        public void MarcarProcessado_AtualizaTimestamp()
        {
            var ev = new WebhookEvento("MP", "evt-1", "x", "{}");
            ev.WhEvProcessadoEm.Should().BeNull();
            ev.MarcarProcessado();
            ev.WhEvProcessadoEm.Should().NotBeNull();
            ev.WhEvProcessadoEm!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }
    }
}

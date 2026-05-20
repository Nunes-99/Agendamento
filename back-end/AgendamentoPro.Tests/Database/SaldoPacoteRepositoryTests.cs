using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Tests.Database
{
    /// <summary>
    /// SaldoPacoteRepository.GetSaldoValidoAsync é central no fluxo de criar
    /// agendamento (decide se cliente paga ou debita pacote). Os tests cobrem:
    /// filtra por tenant/cliente/serviço, ordena pelo que expira mais cedo,
    /// não retorna saldo com quantidade zerada nem expirado.
    /// </summary>
    public class SaldoPacoteRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly DbContextOptions<AgendamentoProDbContext> _opts;
        private readonly TestDb.SeedIds _ids;

        public SaldoPacoteRepositoryTests()
        {
            (_conn, _opts) = TestDb.Create();
            _ids = TestDb.Seed(_opts);
        }

        public void Dispose() => _conn.Dispose();

        private async Task<PacotePrePago> SeedPacoteAsync(int validadeDias = 90, int qtd = 5)
        {
            using var ctx = new AgendamentoProDbContext(_opts);
            var p = new PacotePrePago(_ids.TenantId, _ids.ServicoId,
                "Combo 5 lavagens", qtd, preco: 200m, validadeDias: validadeDias);
            ctx.PacotesPrePagos.Add(p);
            await ctx.SaveChangesAsync();
            return p;
        }

        private async Task<SaldoPacote> SeedSaldoAtivoAsync(PacotePrePago pacote, int? quantidadeRestante = null,
            DateTime? expira = null)
        {
            using var ctx = new AgendamentoProDbContext(_opts);
            var saldo = new SaldoPacote(_ids.TenantId, _ids.ClienteAId, pacote);
            saldo.Ativar();

            if (quantidadeRestante.HasValue || expira.HasValue)
            {
                if (quantidadeRestante.HasValue)
                    typeof(SaldoPacote).GetProperty(nameof(SaldoPacote.SaldQuantidadeRestante))!
                        .SetValue(saldo, quantidadeRestante.Value);
                if (expira.HasValue)
                    typeof(SaldoPacote).GetProperty(nameof(SaldoPacote.SaldExpiraEm))!
                        .SetValue(saldo, expira.Value);
            }

            ctx.SaldosPacote.Add(saldo);
            await ctx.SaveChangesAsync();
            return saldo;
        }

        [Fact]
        public async Task GetSaldoValido_SaldoAtivoComQuantidade_Retorna()
        {
            var pacote = await SeedPacoteAsync();
            await SeedSaldoAtivoAsync(pacote);

            using var ctx = new AgendamentoProDbContext(_opts);
            var repo = new SaldoPacoteRepository(ctx);

            var saldo = await repo.GetSaldoValidoAsync(_ids.TenantId, _ids.ClienteAId, _ids.ServicoId);

            saldo.Should().NotBeNull();
            saldo!.SaldQuantidadeRestante.Should().Be(5);
            saldo.PodeUsar().Should().BeTrue();
        }

        [Fact]
        public async Task GetSaldoValido_QuantidadeZero_NaoRetorna()
        {
            var pacote = await SeedPacoteAsync();
            await SeedSaldoAtivoAsync(pacote, quantidadeRestante: 0);

            using var ctx = new AgendamentoProDbContext(_opts);
            var repo = new SaldoPacoteRepository(ctx);

            var saldo = await repo.GetSaldoValidoAsync(_ids.TenantId, _ids.ClienteAId, _ids.ServicoId);
            saldo.Should().BeNull();
        }

        [Fact]
        public async Task GetSaldoValido_SaldoExpirado_NaoRetorna()
        {
            var pacote = await SeedPacoteAsync();
            await SeedSaldoAtivoAsync(pacote, expira: DateTime.UtcNow.AddDays(-1));

            using var ctx = new AgendamentoProDbContext(_opts);
            var repo = new SaldoPacoteRepository(ctx);

            var saldo = await repo.GetSaldoValidoAsync(_ids.TenantId, _ids.ClienteAId, _ids.ServicoId);
            saldo.Should().BeNull();
        }

        [Fact]
        public async Task GetSaldoValido_OutroCliente_NaoRetorna()
        {
            var pacote = await SeedPacoteAsync();
            await SeedSaldoAtivoAsync(pacote);

            using var ctx = new AgendamentoProDbContext(_opts);
            var repo = new SaldoPacoteRepository(ctx);

            var saldo = await repo.GetSaldoValidoAsync(_ids.TenantId, _ids.ClienteBId, _ids.ServicoId);
            saldo.Should().BeNull();
        }

        [Fact]
        public async Task GetSaldoValido_DoisSaldos_PreferiOQueExpiraPrimeiro()
        {
            var pacote = await SeedPacoteAsync();
            var antigo = await SeedSaldoAtivoAsync(pacote, expira: DateTime.UtcNow.AddDays(5));
            var novo = await SeedSaldoAtivoAsync(pacote, expira: DateTime.UtcNow.AddDays(30));

            using var ctx = new AgendamentoProDbContext(_opts);
            var repo = new SaldoPacoteRepository(ctx);

            var saldo = await repo.GetSaldoValidoAsync(_ids.TenantId, _ids.ClienteAId, _ids.ServicoId);
            saldo!.SaldId.Should().Be(antigo.SaldId);
        }

        [Fact]
        public async Task DebitarPersistido_ReduzQuantidadeEFazProximoBuscaPularSeZerou()
        {
            var pacote = await SeedPacoteAsync(qtd: 2);
            // Força quantidade restante = 1 via reflexão (entity exige mín. 2 na construção)
            await SeedSaldoAtivoAsync(pacote, quantidadeRestante: 1);

            // 1ª busca → debita → fica em zero
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var repo = new SaldoPacoteRepository(ctx);
                var s = await repo.GetSaldoValidoAsync(_ids.TenantId, _ids.ClienteAId, _ids.ServicoId);
                s.Should().NotBeNull();
                s!.Debitar().Should().BeTrue();
                await repo.UpdateAsync(s);
                await ctx.SaveChangesAsync();
            }

            // 2ª busca → não retorna (quantidade 0)
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var repo = new SaldoPacoteRepository(ctx);
                var s = await repo.GetSaldoValidoAsync(_ids.TenantId, _ids.ClienteAId, _ids.ServicoId);
                s.Should().BeNull();
            }
        }
    }
}

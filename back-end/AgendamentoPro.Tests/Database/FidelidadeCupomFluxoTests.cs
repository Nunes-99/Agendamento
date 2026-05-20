using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Tests.Database
{
    /// <summary>
    /// Cobre o ciclo "ganhar pontos → trocar por cupom → cupom é utilizável".
    /// A geração de cupom hoje vive em FidelidadeController.TrocarPorCupom; aqui
    /// reproduzimos a mesma regra (100 pts = R$10, uso único, 60 dias) contra
    /// um banco SQLite in-memory pra garantir que as invariantes batem com o domínio.
    /// </summary>
    public class FidelidadeCupomFluxoTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly DbContextOptions<AgendamentoProDbContext> _opts;
        private readonly TestDb.SeedIds _ids;

        public FidelidadeCupomFluxoTests()
        {
            (_conn, _opts) = TestDb.Create();
            _ids = TestDb.Seed(_opts);
        }

        public void Dispose() => _conn.Dispose();

        private static (string codigo, decimal valor, Cupom cupom) GerarCupomDeTroca(
            int tenantId, int clienteId, int pontos)
        {
            // Replica a regra do FidelidadeController.TrocarPorCupom.
            var valor = Math.Round(pontos / 10m, 2);
            var codigo = $"FID-{clienteId}-{Guid.NewGuid().ToString("N")[..8]}";
            var cupom = new Cupom(tenantId, codigo, TipoDesconto.ValorFixo, valor,
                DateTime.UtcNow, DateTime.UtcNow.AddDays(60), usosMaximos: 1);
            return (codigo, valor, cupom);
        }

        [Fact]
        public async Task Trocar100Pontos_GeraCupom10ReaisUsoUnico_PersisteEhUtilizavel()
        {
            // Arrange: cliente com 150 pontos
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var pts = new PontosFidelidade(_ids.TenantId, _ids.ClienteAId);
                pts.Creditar(150);
                ctx.PontosFidelidade.Add(pts);
                await ctx.SaveChangesAsync();
            }

            // Act: debita 100 pts e cria cupom equivalente
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var pts = await ctx.PontosFidelidade.FirstAsync(
                    p => p.R_CliId == _ids.ClienteAId);
                pts.Debitar(100).Should().BeTrue();

                var (_, valor, cupom) = GerarCupomDeTroca(_ids.TenantId, _ids.ClienteAId, 100);
                valor.Should().Be(10m);
                ctx.Cupons.Add(cupom);
                await ctx.SaveChangesAsync();
            }

            // Assert: saldo desceu, cupom é válido, valor fixo é aplicado corretamente
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var pts = await ctx.PontosFidelidade.AsNoTracking()
                    .FirstAsync(p => p.R_CliId == _ids.ClienteAId);
                pts.PtsSaldo.Should().Be(50);

                var cupom = await ctx.Cupons.AsNoTracking().FirstAsync();
                cupom.CupTipo.Should().Be(TipoDesconto.ValorFixo);
                cupom.CupValor.Should().Be(10m);
                cupom.CupUsosMaximos.Should().Be(1);
                cupom.EhValido(DateTime.UtcNow).Should().BeTrue();

                // Aplicado num atendimento de R$ 50 → cliente paga R$ 40
                cupom.CalcularDesconto(50m).Should().Be(40m);

                // Após registrar uso, deixa de ser válido (limite = 1)
                cupom.RegistrarUso();
                cupom.EhValido(DateTime.UtcNow).Should().BeFalse();
            }
        }

        [Fact]
        public async Task TentarTrocarMaisPontosDoQueSaldo_DebitaRetornaFalse_NenhumCupomCriado()
        {
            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var pts = new PontosFidelidade(_ids.TenantId, _ids.ClienteAId);
                pts.Creditar(30);
                ctx.PontosFidelidade.Add(pts);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                var pts = await ctx.PontosFidelidade.FirstAsync();
                pts.Debitar(100).Should().BeFalse();
                pts.PtsSaldo.Should().Be(30); // não alterou
                // Sem cupom criado — o caller (controller) aborta antes do Add
            }

            using (var ctx = new AgendamentoProDbContext(_opts))
            {
                (await ctx.Cupons.CountAsync()).Should().Be(0);
            }
        }
    }
}

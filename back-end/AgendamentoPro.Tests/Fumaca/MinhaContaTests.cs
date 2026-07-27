#nullable enable
using System.Net;
using AgendamentoPro.Core.Interfaces.Services;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AgendamentoPro.Tests.Fumaca
{
    /// <summary>
    /// "Minha Conta" é a área do cliente final — os próprios agendamentos, pacotes
    /// e pontos, atrás de um token de cliente.
    ///
    /// A listagem de agendamentos devolvia 500. A causa era a mesma que já tinha
    /// derrubado a página de planos: <c>ORDER BY</c> em tipo que o SQLite não
    /// ordena — lá <c>decimal</c>, aqui <c>TimeSpan</c>. É um erro que só aparece
    /// em tempo de execução, no provider padrão, e some de qualquer revisão de
    /// código: a consulta parece perfeitamente razoável.
    ///
    /// O token é cunhado direto pelo <see cref="ITokenService"/>, e não obtido pelo
    /// fluxo de OTP. É de propósito: o que este teste guarda são os ENDPOINTS da
    /// área do cliente, não o login. O OTP tem cobertura própria em OtpUseCaseTests,
    /// e amarrá-lo aqui só acrescentava uma dependência do ambiente (o código de
    /// verificação só volta em Development) que tornava o teste intermitente sob
    /// paralelismo — sem nada a ver com o que se quer verificar.
    /// </summary>
    [Collection(ColecaoApi.Nome)]
    public class MinhaContaTests
    {
        private readonly ApiDeTeste _api;

        public MinhaContaTests(ApiDeTeste api) => _api = api;

        [Fact]
        public async Task As_telas_da_conta_do_cliente_abrem()
        {
            var admin = _api.CreateClient();
            var slug = await _api.CriarTenantAsync(admin, comDadosDeExemplo: true);

            // Cunha um token de cliente para este tenant, direto pelo serviço de
            // token — sem passar pelo OTP. O tenant e ao menos um cliente já existem
            // (o seed de exemplo os criou); pego-os do banco, ignorando os filtros
            // de tenant/soft-delete porque aqui é uma consulta de infraestrutura,
            // fora de qualquer requisição.
            using var scope = _api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendamentoProDbContext>();
            var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();

            var tenant = await db.Tenants.IgnoreQueryFilters()
                .FirstAsync(t => t.TenSlug == slug);
            var cliente = await db.Clientes.IgnoreQueryFilters()
                .FirstAsync(c => c.R_TenId == tenant.TenId);

            var (token, _) = tokens.GerarTokenCliente(cliente.CliId, tenant.TenId, slug);

            var cli = _api.CreateClient();
            cli.DefaultRequestHeaders.Authorization = new("Bearer", token);

            foreach (var rota in new[]
                     {
                         "minha-conta",
                         "minha-conta/agendamentos",
                         "minha-conta/pacotes",
                         "minha-conta/fidelidade",
                     })
            {
                var r = await cli.GetAsync($"/api/v1/t/{slug}/{rota}");
                r.StatusCode.Should()
                    .Be(HttpStatusCode.OK,
                        $"'{rota}' é uma das telas da conta do cliente e precisa abrir");
            }
        }
    }
}

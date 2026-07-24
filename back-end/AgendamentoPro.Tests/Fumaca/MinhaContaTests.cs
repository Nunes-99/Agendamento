#nullable enable
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace AgendamentoPro.Tests.Fumaca
{
    /// <summary>
    /// "Minha Conta" é a área do cliente final: ele entra com código por WhatsApp
    /// (sem senha, sem cadastro) e vê os próprios agendamentos, pacotes e pontos.
    ///
    /// A listagem de agendamentos devolvia 500. A causa era a mesma que já tinha
    /// derrubado a página de planos: <c>ORDER BY</c> em tipo que o SQLite não
    /// ordena — lá <c>decimal</c>, aqui <c>TimeSpan</c>. É um erro que só aparece
    /// em tempo de execução, no provider padrão, e some de qualquer revisão de
    /// código: a consulta parece perfeitamente razoável.
    /// </summary>
    [Collection(ColecaoApi.Nome)]
    public class MinhaContaTests
    {
        private readonly ApiDeTeste _api;

        public MinhaContaTests(ApiDeTeste api) => _api = api;

        [Fact]
        public async Task O_cliente_entra_por_codigo_e_ve_a_propria_conta()
        {
            var admin = _api.CreateClient();
            var slug = await _api.CriarTenantAsync(admin, comDadosDeExemplo: true);

            var cliente = _api.CreateClient();
            const string telefone = "11933332222";

            // Em desenvolvimento o código volta na resposta (não há WhatsApp).
            var pedido = await cliente.PostAsJsonAsync(
                $"/api/v1/t/{slug}/otp/solicitar", new { telefone });
            pedido.StatusCode.Should().Be(HttpStatusCode.OK);
            var desafio = await pedido.Content.ReadFromJsonAsync<RespostaOtp>();
            desafio!.CodigoDev.Should().NotBeNullOrWhiteSpace(
                "sem WhatsApp configurado, o código precisa voltar aqui para dar para testar");

            var validacao = await cliente.PostAsJsonAsync(
                $"/api/v1/t/{slug}/otp/validar",
                new { telefone, codigo = desafio.CodigoDev });
            validacao.StatusCode.Should().Be(HttpStatusCode.OK);
            var sessao = await validacao.Content.ReadFromJsonAsync<RespostaOtp>();
            sessao!.Token.Should().NotBeNullOrWhiteSpace();

            cliente.DefaultRequestHeaders.Authorization = new("Bearer", sessao.Token);

            // As quatro telas da área do cliente precisam abrir.
            foreach (var rota in new[]
                     {
                         "minha-conta",
                         "minha-conta/agendamentos",
                         "minha-conta/pacotes",
                         "minha-conta/fidelidade",
                     })
            {
                var r = await cliente.GetAsync($"/api/v1/t/{slug}/{rota}");
                r.StatusCode.Should()
                    .Be(HttpStatusCode.OK,
                        $"'{rota}' é uma das telas da conta do cliente e precisa abrir");
            }
        }

        private class RespostaOtp
        {
            public string? CodigoDev { get; set; }
            public string? Token { get; set; }
        }
    }
}

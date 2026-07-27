#nullable enable
using System.Net;
using System.Text.Json;
using AgendamentoPro.Infrastructure.Services.Pagamento;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgendamentoPro.Tests.Services
{
    /// <summary>
    /// O corpo que sobe para o Mercado Pago ao criar a assinatura.
    ///
    /// Este é o único ponto do modelo "primeiro mês grátis" que NÃO dá para
    /// validar sem uma conta real do Mercado Pago: se o `free_trial` sair errado
    /// ou faltar, o cartão da oficina é cobrado no primeiro mês — exatamente o
    /// oposto do que foi decidido, e um erro que só apareceria na fatura de um
    /// cliente de verdade. Por isso o formato é travado aqui.
    /// </summary>
    public class MercadoPagoAssinaturaTests
    {
        // O token vai por CONFIG, não por variável de ambiente. Variável de ambiente
        // é global do processo, e o host das outras suítes roda em paralelo — mexer
        // nela aqui contaminava testes que nada têm a ver com pagamento.

        /// <summary>Handler que guarda o corpo enviado e responde um preapproval plausível.</summary>
        private sealed class CapturaHandler : HttpMessageHandler
        {
            public string? CorpoEnviado { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken ct)
            {
                CorpoEnviado = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(
                        """{"id":"pre-1","init_point":"https://mp/x","next_payment_date":"2026-08-24T00:00:00.000-03:00"}"""),
                };
            }
        }

        private static (MercadoPagoAssinaturaService svc, CapturaHandler captura) Criar()
        {
            var captura = new CapturaHandler();
            var http = new HttpClient(captura);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MercadoPago:AccessToken"] = "TEST-token",
                })
                .Build();
            var svc = new MercadoPagoAssinaturaService(
                http, config, new NullLogger<MercadoPagoAssinaturaService>());
            return (svc, captura);
        }

        [Fact]
        public async Task ComTrial_o_payload_leva_free_trial_de_um_mes()
        {
            var (svc, captura) = Criar();

            await svc.CriarPreapprovalAsync(
                tenantId: 1, assinaturaId: 1, valor: 29.90m,
                descricao: "Plano", payerEmail: "dono@oficina.com",
                backUrl: "https://app/back", trialMeses: 1);

            using var doc = JsonDocument.Parse(captura.CorpoEnviado!);
            var recorrencia = doc.RootElement.GetProperty("auto_recurring");

            recorrencia.TryGetProperty("free_trial", out var trial)
                .Should().BeTrue("é o free_trial que adia a primeira cobrança");
            trial.GetProperty("frequency").GetInt32().Should().Be(1);
            trial.GetProperty("frequency_type").GetString().Should().Be("months");

            // A recorrência normal continua mensal e com o valor do plano.
            recorrencia.GetProperty("frequency_type").GetString().Should().Be("months");
            recorrencia.GetProperty("transaction_amount").GetDouble().Should().Be(29.90);
        }

        [Fact]
        public async Task SemTrial_o_payload_NAO_leva_free_trial()
        {
            // free_trial de zero mês faz o Mercado Pago recusar; quando não há
            // período grátis, a chave simplesmente não vai.
            var (svc, captura) = Criar();

            await svc.CriarPreapprovalAsync(
                tenantId: 1, assinaturaId: 1, valor: 29.90m,
                descricao: "Plano", payerEmail: "dono@oficina.com",
                backUrl: "https://app/back", trialMeses: 0);

            using var doc = JsonDocument.Parse(captura.CorpoEnviado!);
            doc.RootElement.GetProperty("auto_recurring")
                .TryGetProperty("free_trial", out _)
                .Should().BeFalse();
        }
    }
}

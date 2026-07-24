#nullable enable
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace AgendamentoPro.Tests.Fumaca
{
    /// <summary>
    /// A rota pública de agendamento é <c>[AllowAnonymous]</c> — é por onde o
    /// cliente final marca horário, sem conta e sem login. Tudo o que ela aceita,
    /// aceita de qualquer um na internet.
    ///
    /// O caso aqui foi encontrado exercitando o fluxo de verdade: mandando
    /// <c>formaPagamento = Dinheiro</c> nessa rota, o agendamento pulava o
    /// gateway e nascia CONFIRMADO. Ou seja, dava para reservar horário sem pagar
    /// nada — o sinal de 20%, que existe para reduzir falta, contornado com um
    /// campo no corpo da requisição. E automatizável: a agenda inteira podia ser
    /// ocupada de graça, deixando o cliente pagante de fora.
    ///
    /// O comentário no código já dizia "Dinheiro só admin". Só que dizer não é
    /// impedir.
    /// </summary>
    [Collection(ColecaoApi.Nome)]
    public class AgendamentoPublicoTests
    {
        private readonly ApiDeTeste _api;

        public AgendamentoPublicoTests(ApiDeTeste api) => _api = api;

        private const int Dinheiro = 3; // FormaPagamento.Dinheiro
        private const int Pix = 2;

        private async Task<(string slug, int servicoId, string data, string hora, int recursoId)> PrepararAsync()
        {
            var cliente = _api.CreateClient();
            var slug = await _api.CriarTenantAsync(cliente, comDadosDeExemplo: true);

            var anonimo = _api.CreateClient();
            var servicos = await anonimo.GetFromJsonAsync<List<ServicoResumo>>(
                $"/api/v1/t/{slug}/servicos");
            var servico = servicos!.First();

            // Uma data à frente, para escapar da antecedência mínima.
            var data = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd");
            var slots = await anonimo.GetFromJsonAsync<List<SlotResumo>>(
                $"/api/v1/t/{slug}/slots?servicoId={servico.Id}&data={data}");
            var slot = slots!.First();

            return (slug, servico.Id, data, slot.HoraInicio, slot.RecursoId);
        }

        private static object Corpo(int servicoId, int recursoId, string data, string hora, int forma) =>
            new
            {
                servicoId,
                recursoId,
                data,
                horaInicio = hora,
                cliente = new
                {
                    nome = "Cliente Anônimo",
                    telefone = "11955554444",
                    email = "anonimo@teste.local",
                },
                formaPagamento = forma,
            };

        [Fact]
        public async Task Ninguem_marca_horario_de_graca_pela_rota_publica()
        {
            var (slug, servicoId, data, hora, recursoId) = await PrepararAsync();
            var anonimo = _api.CreateClient();

            var r = await anonimo.PostAsJsonAsync(
                $"/api/v1/t/{slug}/agendamentos",
                Corpo(servicoId, recursoId, data, hora, Dinheiro)
            );

            r.StatusCode.Should()
                .Be(HttpStatusCode.BadRequest,
                    "dinheiro é lançamento da oficina; pela rua, isso seria reservar sem pagar");

            var corpo = await r.Content.ReadAsStringAsync();
            corpo.Should()
                .Contain("dinheiro", "a mensagem precisa explicar o motivo a quem chamou");
        }

        [Fact]
        public async Task A_forma_legitima_continua_passando_pelo_caminho_do_gateway()
        {
            // Sem gateway configurado no ambiente de teste, PIX falha ao criar a
            // cobrança — e é isso mesmo que se quer provar: a forma legítima VAI
            // ao gateway, em vez de ser confirmada de graça. O que não pode é
            // devolver 200 sem cobrança nenhuma.
            var (slug, servicoId, data, hora, recursoId) = await PrepararAsync();
            var anonimo = _api.CreateClient();

            var r = await anonimo.PostAsJsonAsync(
                $"/api/v1/t/{slug}/agendamentos",
                Corpo(servicoId, recursoId, data, hora, Pix)
            );

            r.StatusCode.Should().NotBe(HttpStatusCode.OK,
                "sem gateway não há cobrança, e sem cobrança não pode haver agendamento confirmado");
        }

        private class ServicoResumo
        {
            public int Id { get; set; }
        }

        private class SlotResumo
        {
            public string HoraInicio { get; set; } = "";
            public int RecursoId { get; set; }
        }
    }
}

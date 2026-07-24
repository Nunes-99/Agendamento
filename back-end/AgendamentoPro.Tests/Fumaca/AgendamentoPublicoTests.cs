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

        /// <summary>
        /// Mensagem de erro da resposta, já desescapada.
        ///
        /// Ler o corpo como texto puro não serve: o serializador escapa
        /// não-ASCII, e "indisponível" chega como "indisponível". Procurar a
        /// palavra no texto cru falha por um motivo que nada tem a ver com o
        /// comportamento sob teste.
        /// </summary>
        private static async Task<string> MensagemDe(HttpResponseMessage r)
        {
            var doc = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
            return doc!.RootElement.TryGetProperty("message", out var m)
                ? m.GetString() ?? ""
                : doc.RootElement.GetProperty("detail").GetString() ?? "";
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

            (await MensagemDe(r))
                .Should()
                .Contain("dinheiro", "a mensagem precisa explicar o motivo a quem chamou");
        }

        [Fact]
        public async Task Sem_gateway_o_cliente_recebe_explicacao_e_nao_erro_de_servidor()
        {
            // Sem gateway configurado, PIX não pode virar agendamento confirmado.
            // O que se cobra aqui é COMO isso é dito: erro de domínio (400) com
            // texto que o cliente entende, e não um 500 "Erro interno do servidor"
            // que não ajuda ninguém — nem quem tentou agendar, nem quem dá suporte.
            var (slug, servicoId, data, hora, recursoId) = await PrepararAsync();
            var anonimo = _api.CreateClient();

            var r = await anonimo.PostAsJsonAsync(
                $"/api/v1/t/{slug}/agendamentos",
                Corpo(servicoId, recursoId, data, hora, Pix)
            );

            r.StatusCode.Should()
                .Be(HttpStatusCode.BadRequest,
                    "faltar configuração de pagamento não é falha de servidor");
            (await MensagemDe(r))
                .Should()
                .Contain("indisponível", "o cliente precisa entender o que fazer");
        }

        [Fact]
        public async Task Falha_na_cobranca_NAO_deixa_o_horario_bloqueado()
        {
            // O horário é o estoque desta oficina. Se uma cobrança que não
            // completou deixar um agendamento para trás, aquele horário fica
            // reservado para alguém que nunca pagou — e o cliente seguinte, que
            // pagaria, encontra a agenda cheia.
            //
            // Este teste guarda o comportamento durante a mudança que tirou a
            // chamada ao gateway de dentro da transação do banco.
            var (slug, servicoId, data, hora, recursoId) = await PrepararAsync();
            var anonimo = _api.CreateClient();

            var r = await anonimo.PostAsJsonAsync(
                $"/api/v1/t/{slug}/agendamentos",
                Corpo(servicoId, recursoId, data, hora, Pix)
            );
            r.IsSuccessStatusCode.Should().BeFalse("não há gateway configurado no teste");

            var slotsDepois = await anonimo.GetFromJsonAsync<List<SlotResumo>>(
                $"/api/v1/t/{slug}/slots?servicoId={servicoId}&data={data}"
            );

            slotsDepois!
                .Should()
                .Contain(s => s.HoraInicio == hora,
                    "a cobrança falhou, então o horário tem que continuar à venda");
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

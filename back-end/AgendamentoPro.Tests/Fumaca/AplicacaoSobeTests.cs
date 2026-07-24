#nullable enable
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AgendamentoPro.Tests.Fumaca
{
    /// <summary>
    /// Sobe a aplicação DE VERDADE e percorre o caminho crítico.
    ///
    /// Este arquivo existe por causa de dois defeitos que passaram por 285 testes
    /// verdes sem serem notados, porque todos eles montam seus objetos à mão:
    ///
    ///  1. Uma dependência não registrada no contêiner derrubava a API no boot.
    ///  2. Um índice único mais rígido que a regra de negócio tornava IMPOSSÍVEL
    ///     criar um tenant — ou seja, cadastrar um cliente novo do SaaS.
    ///
    /// Os dois eram invisíveis para teste de unidade e óbvios para qualquer um que
    /// subisse a aplicação. É exatamente essa lacuna que este teste cobre: se a
    /// aplicação não sobe, ou se um cliente novo não consegue entrar, quebra aqui.
    /// </summary>
    [Collection(ColecaoApi.Nome)]
    public class AplicacaoSobeTests
    {
        private readonly ApiDeTeste _api;

        public AplicacaoSobeTests(ApiDeTeste api) => _api = api;

        [Fact]
        public async Task A_aplicacao_sobe_e_responde()
        {
            // Se alguma dependência não estiver registrada, o host morre ao
            // construir o contêiner e nem chega aqui.
            var cliente = _api.CreateClient();

            var r = await cliente.GetAsync("/api/health/live");

            r.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task O_superadmin_semeado_no_boot_consegue_entrar()
        {
            var cliente = _api.CreateClient();

            var r = await cliente.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email = ApiDeTeste.SuperEmail, senha = ApiDeTeste.SuperSenha }
            );

            r.StatusCode.Should().Be(HttpStatusCode.OK);
            var corpo = await r.Content.ReadFromJsonAsync<RespostaLogin>();
            corpo!.AccessToken.Should().NotBeNullOrWhiteSpace();
            corpo.Perfil.Should().Be("SuperAdmin");
        }

        [Fact]
        public async Task Um_cliente_novo_do_SaaS_consegue_ser_cadastrado_e_entrar()
        {
            // O caminho que estava quebrado: criar tenant dispara o seeder de
            // dados-exemplo, que gera dezenas de agendamentos. Qualquer colisão
            // ali derrubava a transação inteira e o cadastro não acontecia.
            var cliente = _api.CreateClient();
            var token = await _api.TokenDoSuperAdminAsync(cliente);
            cliente.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var slug = "fumaca-" + Guid.NewGuid().ToString("N")[..8];
            var criacao = await cliente.PostAsJsonAsync(
                "/api/v1/tenants",
                new
                {
                    nome = "Oficina da Fumaça",
                    slug,
                    segmento = "Lava-rápido",
                    email = $"{slug}@teste.local",
                    telefone = "11999990000",
                    adminNome = "Dono da Fumaça",
                    adminEmail = $"dono-{slug}@teste.local",
                    adminSenha = "Fumaca!2026",
                }
            );

            criacao
                .StatusCode.Should()
                .Match(s => s == HttpStatusCode.OK || s == HttpStatusCode.Created,
                    "criar um tenant é o primeiro passo de todo cliente novo — se falhar aqui, "
                        + "ninguém entra no SaaS. Corpo: "
                        + await criacao.Content.ReadAsStringAsync());

            // E o admin recém-criado precisa conseguir entrar de fato.
            var semToken = _api.CreateClient();
            var login = await semToken.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email = $"dono-{slug}@teste.local", senha = "Fumaca!2026" }
            );

            login.StatusCode.Should().Be(HttpStatusCode.OK);
            var corpo = await login.Content.ReadFromJsonAsync<RespostaLogin>();
            corpo!.TenantSlug.Should().Be(slug);
        }

        [Fact]
        public async Task Cliente_novo_nasce_SEM_dado_ficticio()
        {
            // Um cliente pagante não pode receber a conta dele com agendamentos e
            // clientes inventados dentro — ele não tem como saber o que é real.
            var cliente = _api.CreateClient();
            var slug = await _api.CriarTenantAsync(cliente, comDadosDeExemplo: false);

            var anonimo = _api.CreateClient();
            var servicos = await anonimo.GetFromJsonAsync<List<object>>($"/api/v1/t/{slug}/servicos");

            servicos.Should().BeEmpty("o catálogo de um cliente novo começa vazio");
        }

        [Fact]
        public async Task Com_dados_de_exemplo_o_seeder_roda_inteiro()
        {
            // Este é o teste que pega o defeito do índice único: o seeder cancela
            // parte dos agendamentos e depois sorteia os mesmos horários. Se o
            // índice voltar a ser mais rígido que a regra de negócio, quebra aqui.
            var cliente = _api.CreateClient();
            var slug = await _api.CriarTenantAsync(cliente, comDadosDeExemplo: true);

            var anonimo = _api.CreateClient();
            var servicos = await anonimo.GetFromJsonAsync<List<object>>($"/api/v1/t/{slug}/servicos");

            servicos.Should().NotBeEmpty("com a flag ligada, o catálogo de demonstração é semeado");
        }

        [Fact]
        public async Task A_home_publica_do_tenant_abre_sem_login()
        {
            var cliente = _api.CreateClient();
            var token = await _api.TokenDoSuperAdminAsync(cliente);
            cliente.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var slug = "publica-" + Guid.NewGuid().ToString("N")[..8];
            await cliente.PostAsJsonAsync(
                "/api/v1/tenants",
                new
                {
                    nome = "Oficina Pública",
                    slug,
                    segmento = "Lava-rápido",
                    email = $"{slug}@teste.local",
                    telefone = "11999990000",
                    adminNome = "Dono",
                    adminEmail = $"dono-{slug}@teste.local",
                    adminSenha = "Publica!2026",
                }
            );

            // Sem Authorization nenhum: é o que o cliente final enxerga.
            var anonimo = _api.CreateClient();
            var r = await anonimo.GetAsync($"/api/v1/tenants/public/by-slug/{slug}");

            r.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private class RespostaLogin
        {
            public string? AccessToken { get; set; }
            public string? Perfil { get; set; }
            public string? TenantSlug { get; set; }
        }
    }

    /// <summary>
    /// UMA instância do host para todas as classes de fumaça.
    ///
    /// Não é economia: o <see cref="ApiDeTeste"/> escreve variáveis de AMBIENTE do
    /// processo (o código de produção as lê com Environment.GetEnvironmentVariable,
    /// não por IConfiguration). Com uma instância por classe, o xUnit roda as
    /// classes em paralelo e elas sobrescrevem as variáveis umas das outras —
    /// hosts subindo apontados para o banco errado, falhas que mudam a cada
    /// execução. Uma coleção só, e o problema deixa de existir.
    /// </summary>
    [CollectionDefinition(Nome)]
    public class ColecaoApi : ICollectionFixture<ApiDeTeste>
    {
        public const string Nome = "API de fumaça";
    }

    /// <summary>
    /// Host de teste: banco SQLite próprio e descartável, Hangfire em memória
    /// (jobs de verdade não têm o que fazer aqui) e credenciais de super-admin
    /// conhecidas, para o teste conseguir entrar.
    /// </summary>
    public class ApiDeTeste : WebApplicationFactory<Program>
    {
        public const string SuperEmail = "fumaca@teste.local";
        public const string SuperSenha = "Fumaca!2026Admin";

        private readonly string _banco = Path.Combine(
            Path.GetTempPath(),
            $"agendamento-fumaca-{Guid.NewGuid():N}.db"
        );

        public ApiDeTeste()
        {
            // Lidas com Environment.GetEnvironmentVariable no código de produção,
            // então precisam existir no processo — não basta IConfiguration.
            Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={_banco}");
            Environment.SetEnvironmentVariable("HANGFIRE_STORAGE", "Memory");
            Environment.SetEnvironmentVariable("SUPERADMIN_EMAIL", SuperEmail);
            Environment.SetEnvironmentVariable("SUPERADMIN_PASSWORD", SuperSenha);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseEnvironment("Development");

        /// <summary>Cria um tenant e devolve o slug dele.</summary>
        public async Task<string> CriarTenantAsync(HttpClient cliente, bool comDadosDeExemplo)
        {
            var token = await TokenDoSuperAdminAsync(cliente);
            cliente.DefaultRequestHeaders.Authorization = new("Bearer", token);

            var slug = (comDadosDeExemplo ? "demo-" : "limpo-") + Guid.NewGuid().ToString("N")[..8];
            var r = await cliente.PostAsJsonAsync(
                "/api/v1/tenants",
                new
                {
                    nome = "Oficina " + slug,
                    slug,
                    segmento = "Lava-rápido",
                    email = $"{slug}@teste.local",
                    telefone = "11999990000",
                    adminNome = "Dono",
                    adminEmail = $"dono-{slug}@teste.local",
                    adminSenha = "Fumaca!2026",
                    comDadosDeExemplo,
                }
            );
            r.EnsureSuccessStatusCode();
            return slug;
        }

        private string? _tokenSuper;

        /// <summary>
        /// Token do super-admin, obtido UMA vez e reaproveitado.
        ///
        /// Não é otimização: o rate limit de autenticação é de 5 por minuto por IP,
        /// e no host de teste todas as requisições saem do mesmo IP. Um login por
        /// teste estoura a cota e faz os testes falharem em conjunto enquanto
        /// passam isoladamente — do tipo de falha que se perde horas perseguindo.
        /// </summary>
        public async Task<string> TokenDoSuperAdminAsync(HttpClient cliente)
        {
            if (_tokenSuper != null) return _tokenSuper;

            var r = await cliente.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email = SuperEmail, senha = SuperSenha }
            );
            r.EnsureSuccessStatusCode();
            var doc = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
            return _tokenSuper = doc!.RootElement.GetProperty("accessToken").GetString()!;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            try
            {
                if (File.Exists(_banco)) File.Delete(_banco);
            }
            catch
            {
                // arquivo preso pelo SQLite: o temp do SO limpa depois
            }
        }
    }
}

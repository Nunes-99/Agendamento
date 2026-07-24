#nullable enable
using AgendamentoPro.Core.Interfaces.Services;
using AgendamentoPro.Infrastructure.IoC;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AgendamentoPro.Tests.IoC
{
    /// <summary>
    /// O contêiner tem que conseguir construir tudo o que ele mesmo registra.
    ///
    /// Este teste nasceu de um defeito real: o <c>RecaptchaValidator</c> foi
    /// implementado do Core ao frontend, o <c>LoginUseCase</c> passou a recebê-lo
    /// por construtor — e o registro no contêiner ficou de fora do commit. Os 280
    /// testes continuaram verdes, porque todos montam seus objetos à mão com Moq;
    /// nenhum passava pelo contêiner. Resultado: a API não subia.
    ///
    /// É uma classe de defeito que nenhum teste de unidade pega e que só aparece
    /// no boot — em Development a validação derruba a aplicação; em Production ela
    /// não roda por padrão, então a API sobe e estoura no primeiro login.
    ///
    /// O que este teste faz é exatamente o que o host faz ao subir: constrói o
    /// provedor com validação ligada. Se alguém adicionar uma dependência e
    /// esquecer de registrá-la, quebra aqui, em três segundos.
    /// </summary>
    public class ContainerTests
    {
        /// <summary>
        /// O que a camada de API registra por fora do <c>WireUp</c> e que o
        /// Infrastructure espera encontrar pronto. Mantida deliberadamente
        /// pequena: se esta lista crescer, é sinal de que a fronteira entre as
        /// camadas está vazando.
        /// </summary>
        private static void RegistrarOQueVemDaApi(IServiceCollection services, IConfiguration config)
        {
            services.AddLogging();
            services.AddHttpContextAccessor();
            // O host registra IConfiguration sozinho; num ServiceCollection nu, não.
            services.AddSingleton(config);
            // Implementada com SignalR, que mora na API.
            services.AddSingleton(new Mock<INotificacaoRealtime>().Object);
            // Vem do AddHangfire(), chamado no Program.cs.
            services.AddSingleton(new Mock<Hangfire.IBackgroundJobClient>().Object);
            services.AddSingleton(new Mock<Hangfire.IRecurringJobManager>().Object);
        }

        private static IConfiguration Configuracao() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Database:Provider"] = "Sqlite",
                        ["ConnectionStrings:Default"] = "Data Source=:memory:",
                    }
                )
                .Build();

        [Fact]
        public void Tudo_o_que_o_conteiner_registra_pode_ser_construido()
        {
            var config = Configuracao();
            var services = new ServiceCollection();
            RegistrarOQueVemDaApi(services, config);
            services.WireUp(config);

            // ValidateOnBuild percorre TODOS os descritores e tenta montar a
            // cadeia de construtores de cada um — é o que o host faz no boot.
            var construir = () =>
                services.BuildServiceProvider(
                    new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }
                );

            construir.Should().NotThrow();
        }

        [Fact]
        public void LoginUseCase_resolve_com_todas_as_suas_dependencias()
        {
            // O caso específico que quebrou. Vale à parte do teste acima porque
            // nomeia o culpado: se voltar a falhar, a mensagem já diz onde olhar.
            var config = Configuracao();
            var services = new ServiceCollection();
            RegistrarOQueVemDaApi(services, config);
            services.WireUp(config);

            using var provedor = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateScopes = true }
            );
            using var escopo = provedor.CreateScope();

            escopo
                .ServiceProvider.GetService<Application.Interfaces.Auth.ILoginUseCase>()
                .Should()
                .NotBeNull();
        }
    }
}

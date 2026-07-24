#nullable enable
using AgendamentoPro.Infrastructure.IoC;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgendamentoPro.Tests.IoC
{
    /// <summary>
    /// SQL Server está documentado no README, mas as migrations do projeto foram
    /// todas geradas contra o SQLite — 333 colunas TEXT/INTEGER. Subir com
    /// SqlServer aplicaria um schema inválido, e o estrago só apareceria com
    /// dado de cliente dentro.
    ///
    /// Até existir um conjunto de migrations por provider, a escolha é barrar
    /// cedo e explicar. Estes testes garantem que a porta continua fechada — e
    /// que o SQLite, que é o caminho real, segue aberto.
    /// </summary>
    public class ProviderBancoTests
    {
        private static IServiceCollection Montar(string provider)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Database:Provider"] = provider,
                        ["ConnectionStrings:Default"] = "Data Source=:memory:",
                    }
                )
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(config);
            services.WireUp(config);
            return services;
        }

        [Fact]
        public void SqlServer_falha_alto_e_cedo_com_explicacao()
        {
            var montar = () => Montar("SqlServer");

            montar
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*migrations*")
                .WithMessage("*SQLite*");
        }

        [Fact]
        public void Sqlite_continua_funcionando()
        {
            var montar = () => Montar("Sqlite");

            montar.Should().NotThrow();
        }

        [Fact]
        public void Da_para_assumir_o_risco_conscientemente()
        {
            // A saída de emergência existe para quem quiser experimentar num
            // ambiente descartável — mas exige um ato deliberado.
            Environment.SetEnvironmentVariable("DATABASE_SQLSERVER_EXPERIMENTAL", "true");
            try
            {
                var montar = () => Montar("SqlServer");
                montar.Should().NotThrow();
            }
            finally
            {
                Environment.SetEnvironmentVariable("DATABASE_SQLSERVER_EXPERIMENTAL", null);
            }
        }
    }
}

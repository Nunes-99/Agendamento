using AgendamentoPro.Core.Interfaces.Database;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Infrastructure.Database.Multitenancy
{
    /// <summary>
    /// Inicializa o banco físico de um tenant (modo PerTenant): cria o arquivo .db
    /// e aplica todas as migrations. No-op no modo Shared.
    ///
    /// Chamado pelo SuperAdmin ao "promover" um tenant para isolamento físico,
    /// e automaticamente quando um tenant é criado em ambiente PerTenant.
    /// </summary>
    public class TenantDatabaseInitializer
    {
        private readonly ITenantConnectionFactory _factory;
        private readonly string _provider;
        private readonly ILogger<TenantDatabaseInitializer> _logger;

        public TenantDatabaseInitializer(ITenantConnectionFactory factory, string provider,
            ILogger<TenantDatabaseInitializer> logger)
        {
            _factory = factory;
            _provider = provider;
            _logger = logger;
        }

        public async Task EnsureDatabaseAsync(int tenantId, CancellationToken ct = default)
        {
            if (!_factory.IsPerTenant)
            {
                _logger.LogDebug("EnsureDatabaseAsync: modo Shared, no-op para tenant {Id}.", tenantId);
                return;
            }

            var conn = _factory.GetConnectionString(tenantId);
            var opts = new DbContextOptionsBuilder<AgendamentoProDbContext>();
            if (_provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                opts.UseSqlServer(conn);
            else
                opts.UseSqlite(conn);

            using var ctx = new AgendamentoProDbContext(opts.Options);
            if (ctx.Database.GetMigrations().Any())
            {
                var pendentes = (await ctx.Database.GetPendingMigrationsAsync(ct)).ToList();
                if (pendentes.Count > 0)
                {
                    _logger.LogInformation("Aplicando {Count} migration(s) ao tenant {Id}: {List}",
                        pendentes.Count, tenantId, string.Join(", ", pendentes));
                    await ctx.Database.MigrateAsync(ct);
                }
            }
            else
            {
                await ctx.Database.EnsureCreatedAsync(ct);
            }
            _logger.LogInformation("Banco do tenant {Id} pronto: {Conn}", tenantId, conn);
        }
    }
}

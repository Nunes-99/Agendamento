using AgendamentoPro.Core.Interfaces.Database;

namespace AgendamentoPro.Infrastructure.Database.Multitenancy
{
    /// <summary>
    /// Modo PerTenant (SQLite): cada tenant tem seu próprio arquivo .db, em
    /// `{TenantsPath}/tenant-{id}.db`. O banco compartilhado (auth, registro de
    /// tenants) continua na connection string default — para tenantId null
    /// (login, super-admin), retorna a shared.
    ///
    /// Requisitos para usar em produção:
    ///   - Migration runner deve aplicar migrations em cada arquivo .db a cada deploy
    ///   - Backups precisam tarball-zar o diretório TenantsPath inteiro
    ///   - Atenção ao limite de file descriptors do SO em deploys com muitos tenants
    /// </summary>
    public class PerTenantSqliteConnectionFactory : ITenantConnectionFactory
    {
        private readonly string _sharedConn;
        private readonly string _tenantsPath;

        public PerTenantSqliteConnectionFactory(string sharedConnectionString, string tenantsPath)
        {
            _sharedConn = sharedConnectionString;
            _tenantsPath = tenantsPath;
            Directory.CreateDirectory(_tenantsPath);
        }

        public string Mode => "PerTenant";
        public bool IsPerTenant => true;

        public string GetConnectionString(int? tenantId)
        {
            if (!tenantId.HasValue) return _sharedConn;
            var caminho = Path.Combine(_tenantsPath, $"tenant-{tenantId.Value}.db");
            return $"Data Source={caminho}";
        }

        public bool DatabaseExists(int tenantId)
        {
            var caminho = Path.Combine(_tenantsPath, $"tenant-{tenantId}.db");
            return File.Exists(caminho);
        }
    }
}

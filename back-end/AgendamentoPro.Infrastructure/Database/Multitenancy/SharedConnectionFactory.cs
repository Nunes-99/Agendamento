using AgendamentoPro.Core.Interfaces.Database;

namespace AgendamentoPro.Infrastructure.Database.Multitenancy
{
    /// <summary>
    /// Modo Shared: todos os tenants usam o mesmo banco. Isolamento via foreign key
    /// `R_TenId` em todas as entidades + índices compostos. É o comportamento default
    /// do AgendamentoPro e o recomendado para a maioria dos casos.
    /// </summary>
    public class SharedConnectionFactory : ITenantConnectionFactory
    {
        private readonly string _conn;
        public SharedConnectionFactory(string connectionString) { _conn = connectionString; }

        public string Mode => "Shared";
        public bool IsPerTenant => false;
        public string GetConnectionString(int? tenantId) => _conn;
        public bool DatabaseExists(int tenantId) => true;
    }
}

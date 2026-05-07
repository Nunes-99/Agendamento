namespace AgendamentoPro.Core.Interfaces.Database
{
    /// <summary>
    /// Resolve a connection string usada pelo DbContext em runtime.
    ///
    /// Modos:
    ///   - Shared (default): retorna sempre a connection string global. Mesmo banco para
    ///     todos os tenants (isolamento por tenant_id em foreign keys + índices).
    ///   - PerTenant: retorna `Data Source=tenants/tenant-{id}.db` para SQLite, ou
    ///     `...Database=AgendamentoPro_T{id}...` para SQL Server. Cada tenant tem seu
    ///     próprio banco — isolamento físico, melhor pra LGPD e backups por tenant.
    ///
    /// Para usar PerTenant em produção real é preciso (1) migrar dados existentes pra
    /// bancos individuais, (2) garantir que migrations rodem em todos eles a cada deploy.
    /// O AdminController expõe um endpoint para inicializar/migrar a DB de um tenant.
    /// </summary>
    public interface ITenantConnectionFactory
    {
        /// <summary>Modo atual ("Shared" ou "PerTenant").</summary>
        string Mode { get; }

        /// <summary>True se o factory atribui connection string distinta por tenant.</summary>
        bool IsPerTenant { get; }

        /// <summary>
        /// Retorna a connection string a usar dado o tenant atual. Quando tenantId é null
        /// (login, registro, super-admin sem contexto), volta a connection string compartilhada.
        /// </summary>
        string GetConnectionString(int? tenantId);

        /// <summary>True se o database físico já foi criado (apenas relevante em PerTenant).</summary>
        bool DatabaseExists(int tenantId);
    }
}

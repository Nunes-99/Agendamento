using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework
{
    /// <summary>
    /// Factory usada apenas em design-time (dotnet ef migrations add/update) para que o EF
    /// Tools consiga construir um DbContext sem subir o host completo do AspNetCore.
    /// Em runtime o DbContext continua sendo registrado via AddDbContext em DI.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AgendamentoProDbContext>
    {
        public AgendamentoProDbContext CreateDbContext(string[] args)
        {
            var conn = Environment.GetEnvironmentVariable("MIGRATIONS_CONN_STRING")
                ?? "Data Source=design_time.db";
            var opts = new DbContextOptionsBuilder<AgendamentoProDbContext>()
                .UseSqlite(conn)
                .Options;
            return new AgendamentoProDbContext(opts);
        }
    }
}

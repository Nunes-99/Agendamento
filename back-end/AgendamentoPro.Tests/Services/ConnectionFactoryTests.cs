using AgendamentoPro.Infrastructure.Database.Multitenancy;
using FluentAssertions;

namespace AgendamentoPro.Tests.Services
{
    public class ConnectionFactoryTests : IDisposable
    {
        private readonly string _tempDir;

        public ConnectionFactoryTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "agp-tenants-" + Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
        }

        [Fact]
        public void Shared_RetornaSempreMesmaConnection()
        {
            var f = new SharedConnectionFactory("Data Source=app.db");
            f.Mode.Should().Be("Shared");
            f.IsPerTenant.Should().BeFalse();
            f.GetConnectionString(null).Should().Be("Data Source=app.db");
            f.GetConnectionString(1).Should().Be("Data Source=app.db");
            f.GetConnectionString(999).Should().Be("Data Source=app.db");
            f.DatabaseExists(1).Should().BeTrue();
        }

        [Fact]
        public void PerTenant_TenantNuloUsaShared()
        {
            var f = new PerTenantSqliteConnectionFactory("Data Source=shared.db", _tempDir);
            f.Mode.Should().Be("PerTenant");
            f.IsPerTenant.Should().BeTrue();
            f.GetConnectionString(null).Should().Be("Data Source=shared.db");
        }

        [Fact]
        public void PerTenant_TenantId_GerarConnectionEspecifica()
        {
            var f = new PerTenantSqliteConnectionFactory("Data Source=shared.db", _tempDir);
            var conn1 = f.GetConnectionString(1);
            var conn2 = f.GetConnectionString(2);
            conn1.Should().Contain("tenant-1.db");
            conn2.Should().Contain("tenant-2.db");
            conn1.Should().NotBe(conn2);
        }

        [Fact]
        public void PerTenant_DatabaseExists_FalseQuandoArquivoFalta()
        {
            var f = new PerTenantSqliteConnectionFactory("Data Source=shared.db", _tempDir);
            f.DatabaseExists(123).Should().BeFalse();
        }

        [Fact]
        public void PerTenant_CriaDiretorio_AutomaticamenteNoConstrutor()
        {
            var path = Path.Combine(_tempDir, "subdir");
            new PerTenantSqliteConnectionFactory("Data Source=x", path);
            Directory.Exists(path).Should().BeTrue();
        }
    }
}

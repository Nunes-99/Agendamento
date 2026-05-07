using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Recursos;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Tests.Database
{
    /// <summary>
    /// Helper para criar um DbContext SQLite in-memory e semear FKs
    /// (Tenant, Cliente, Servico, Recurso) para os testes que precisam.
    /// </summary>
    public static class TestDb
    {
        public class SeedIds
        {
            public int TenantId { get; set; }
            public int ClienteAId { get; set; }
            public int ClienteBId { get; set; }
            public int ServicoId { get; set; }
            public int RecursoAId { get; set; }
            public int RecursoBId { get; set; }
        }

        public static (SqliteConnection Conn, DbContextOptions<AgendamentoProDbContext> Opts) Create()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();
            var opts = new DbContextOptionsBuilder<AgendamentoProDbContext>()
                .UseSqlite(conn)
                .Options;
            using var ctx = new AgendamentoProDbContext(opts);
            ctx.Database.EnsureCreated();
            return (conn, opts);
        }

        public static SeedIds Seed(DbContextOptions<AgendamentoProDbContext> opts)
        {
            using var ctx = new AgendamentoProDbContext(opts);

            var tenant = new Tenant("Tenant Teste", "tenant-test", "Lava-rápido", "x@y.com", "11999999999");
            ctx.Tenants.Add(tenant);
            ctx.SaveChanges();

            var clienteA = new Cliente(tenant.TenId, "Cliente A", "a@y.com", "11988887777", null, null);
            var clienteB = new Cliente(tenant.TenId, "Cliente B", "b@y.com", "11988886666", null, null);
            ctx.Clientes.AddRange(clienteA, clienteB);

            var servico = new Servico(tenant.TenId, "Servico Padrão", null, 100m, 60, null, null, 0);
            ctx.Servicos.Add(servico);

            var recursoA = new Recurso(tenant.TenId, "Box A", null, "Box", null, 0);
            var recursoB = new Recurso(tenant.TenId, "Box B", null, "Box", null, 1);
            ctx.Recursos.AddRange(recursoA, recursoB);

            ctx.SaveChanges();

            return new SeedIds
            {
                TenantId = tenant.TenId,
                ClienteAId = clienteA.CliId,
                ClienteBId = clienteB.CliId,
                ServicoId = servico.SerId,
                RecursoAId = recursoA.RecId,
                RecursoBId = recursoB.RecId
            };
        }
    }
}

using AgendamentoPro.Core.Entities.Assinaturas;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Infrastructure.Middlewares;
using AgendamentoPro.Infrastructure.Services.Assinaturas;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace AgendamentoPro.Tests.Middlewares
{
    public class AssinaturaGuardMiddlewareTests
    {
        private static (AssinaturaGuardMiddleware middleware, Action<bool> setNextCalled, Func<bool> wasNextCalled)
            Construir()
        {
            var nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
            return (
                new AssinaturaGuardMiddleware(next),
                v => nextCalled = v,
                () => nextCalled
            );
        }

        private static DefaultHttpContext Request(string path, string metodo = "GET")
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = path;
            ctx.Request.Method = metodo;
            ctx.Response.Body = new MemoryStream();
            return ctx;
        }

        private static Mock<ITenantContext> TenantCtx(int? tid)
        {
            var m = new Mock<ITenantContext>();
            m.SetupGet(c => c.IsResolved).Returns(tid.HasValue);
            m.SetupGet(c => c.TenantId).Returns(tid);
            return m;
        }

        private static Mock<IAssinaturaRepository> AssinaturaRepo(StatusAssinatura? status)
        {
            var m = new Mock<IAssinaturaRepository>();
            if (status.HasValue)
            {
                var ass = new Assinatura(1, 1, "MercadoPago");
                if (status.Value == StatusAssinatura.Atrasada) ass.MarcarAtrasada(DateTime.UtcNow);
                if (status.Value == StatusAssinatura.ReadOnly) { ass.MarcarAtrasada(DateTime.UtcNow); ass.TransicionarReadOnly(DateTime.UtcNow); }
                if (status.Value == StatusAssinatura.Expirada) { ass.MarcarAtrasada(DateTime.UtcNow); ass.TransicionarReadOnly(DateTime.UtcNow); ass.Expirar(DateTime.UtcNow); }
                if (status.Value == StatusAssinatura.Cancelada) ass.Cancelar(DateTime.UtcNow);
                // O guard usa GetUltimaByTenantAsync (sem filtro de status) — o
                // GetByTenantAsync real esconde Cancelada/Expirada e mascarava o bloqueio.
                m.Setup(r => r.GetUltimaByTenantAsync(It.IsAny<int>())).ReturnsAsync(ass);
            }
            else
            {
                m.Setup(r => r.GetUltimaByTenantAsync(It.IsAny<int>())).ReturnsAsync((Assinatura)null);
            }
            return m;
        }

        private static AssinaturaStatusCache NovoCache() => new(new MemoryCache(new MemoryCacheOptions()));

        [Fact]
        public async Task SemTenant_DeixaPassar()
        {
            var (mw, _, wasCalled) = Construir();
            var ctx = Request("/api/v1/admin/agendamentos", "POST");
            var tenant = TenantCtx(null);
            var repo = new Mock<IAssinaturaRepository>();

            await mw.InvokeAsync(ctx, tenant.Object, repo.Object, NovoCache());

            wasCalled().Should().BeTrue();
            repo.Verify(r => r.GetUltimaByTenantAsync(It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData("/api/v1/admin/assinatura")]
        [InlineData("/api/v1/planos")]
        [InlineData("/api/v1/webhooks/assinatura/MercadoPago")]
        [InlineData("/api/v1/auth/login")]
        [InlineData("/api/health/ready")]
        [InlineData("/hubs/notificacoes")]
        public async Task PathWhitelisted_DeixaPassarMesmoEmReadOnly(string path)
        {
            var (mw, _, wasCalled) = Construir();
            var ctx = Request(path, "POST");
            await mw.InvokeAsync(ctx, TenantCtx(1).Object, AssinaturaRepo(StatusAssinatura.ReadOnly).Object, NovoCache());
            wasCalled().Should().BeTrue();
            ctx.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public async Task SemAssinatura_DeixaPassar()
        {
            var (mw, _, wasCalled) = Construir();
            var ctx = Request("/api/v1/admin/agendamentos", "POST");
            await mw.InvokeAsync(ctx, TenantCtx(1).Object, AssinaturaRepo(null).Object, NovoCache());
            wasCalled().Should().BeTrue();
        }

        [Theory]
        [InlineData(StatusAssinatura.Trial)]
        [InlineData(StatusAssinatura.Ativa)]
        [InlineData(StatusAssinatura.Atrasada)]
        public async Task StatusSaudavel_DeixaPassarEscrita(StatusAssinatura status)
        {
            var (mw, _, wasCalled) = Construir();
            var ctx = Request("/api/v1/admin/agendamentos", "POST");
            await mw.InvokeAsync(ctx, TenantCtx(1).Object, AssinaturaRepo(status).Object, NovoCache());
            wasCalled().Should().BeTrue();
        }

        [Theory]
        [InlineData(StatusAssinatura.ReadOnly)]
        [InlineData(StatusAssinatura.Cancelada)]
        [InlineData(StatusAssinatura.Expirada)]
        public async Task StatusBloqueado_AdminWrite_Retorna402(StatusAssinatura status)
        {
            var (mw, _, wasCalled) = Construir();
            var ctx = Request("/api/v1/admin/agendamentos", "POST");
            await mw.InvokeAsync(ctx, TenantCtx(1).Object, AssinaturaRepo(status).Object, NovoCache());
            wasCalled().Should().BeFalse();
            ctx.Response.StatusCode.Should().Be(StatusCodes.Status402PaymentRequired);
        }

        [Theory]
        [InlineData(StatusAssinatura.ReadOnly)]
        [InlineData(StatusAssinatura.Cancelada)]
        [InlineData(StatusAssinatura.Expirada)]
        public async Task StatusBloqueado_AdminLeitura_Passa(StatusAssinatura status)
        {
            var (mw, _, wasCalled) = Construir();
            var ctx = Request("/api/v1/admin/agendamentos", "GET");
            await mw.InvokeAsync(ctx, TenantCtx(1).Object, AssinaturaRepo(status).Object, NovoCache());
            wasCalled().Should().BeTrue();
        }

        [Theory]
        [InlineData(StatusAssinatura.ReadOnly, "GET")]
        [InlineData(StatusAssinatura.ReadOnly, "POST")]
        [InlineData(StatusAssinatura.Cancelada, "GET")]
        [InlineData(StatusAssinatura.Expirada, "POST")]
        public async Task StatusBloqueado_AreaPublica_Retorna503(StatusAssinatura status, string metodo)
        {
            var (mw, _, wasCalled) = Construir();
            var ctx = Request("/api/v1/t/lavacar/agendamentos", metodo);
            await mw.InvokeAsync(ctx, TenantCtx(1).Object, AssinaturaRepo(status).Object, NovoCache());
            wasCalled().Should().BeFalse();
            ctx.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        }

        [Fact]
        public async Task Cache_NaoConsultaRepoEmRequestSubsequente()
        {
            var (mw, _, _) = Construir();
            var repo = AssinaturaRepo(StatusAssinatura.Ativa);
            var cache = NovoCache();

            // Primeira request: 1 query
            await mw.InvokeAsync(Request("/api/v1/admin/agendamentos"), TenantCtx(1).Object, repo.Object, cache);
            // Segunda request mesmo tenant: deve usar cache
            await mw.InvokeAsync(Request("/api/v1/admin/agendamentos"), TenantCtx(1).Object, repo.Object, cache);

            repo.Verify(r => r.GetUltimaByTenantAsync(1), Times.Once);
        }

        [Fact]
        public async Task Cache_InvalidarForcaNovaConsulta()
        {
            var (mw, _, _) = Construir();
            var repo = AssinaturaRepo(StatusAssinatura.Ativa);
            var cache = NovoCache();

            await mw.InvokeAsync(Request("/api/v1/admin/agendamentos"), TenantCtx(1).Object, repo.Object, cache);
            cache.Invalidar(1);
            await mw.InvokeAsync(Request("/api/v1/admin/agendamentos"), TenantCtx(1).Object, repo.Object, cache);

            repo.Verify(r => r.GetUltimaByTenantAsync(1), Times.Exactly(2));
        }
    }
}

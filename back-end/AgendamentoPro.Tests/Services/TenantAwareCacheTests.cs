using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Infrastructure.Services.Cache;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace AgendamentoPro.Tests.Services
{
    public class TenantAwareCacheTests
    {
        private static IMemoryCache NovoCache() => new MemoryCache(new MemoryCacheOptions());

        private static Mock<ITenantContext> MockTenant(int? id, string slug = null)
        {
            var m = new Mock<ITenantContext>();
            m.SetupGet(t => t.IsResolved).Returns(id.HasValue);
            m.SetupGet(t => t.TenantId).Returns(id);
            m.SetupGet(t => t.Slug).Returns(slug);
            return m;
        }

        [Fact]
        public void Set_Get_NoMesmoTenant_RetornaValor()
        {
            var cache = NovoCache();
            var ctx = MockTenant(1);
            var sut = new TenantAwareMemoryCache(cache, ctx.Object);
            sut.Set("clientes", new List<string> { "X" }, TimeSpan.FromMinutes(5));
            sut.Get<List<string>>("clientes").Should().NotBeNull().And.HaveCount(1);
        }

        [Fact]
        public void Get_OutroTenant_NaoEnxergaValorDoPrimeiro()
        {
            var cache = NovoCache();
            var t1 = MockTenant(1);
            var t2 = MockTenant(2);
            var s1 = new TenantAwareMemoryCache(cache, t1.Object);
            var s2 = new TenantAwareMemoryCache(cache, t2.Object);

            s1.Set("clientes", new List<string> { "X" }, TimeSpan.FromMinutes(5));
            s2.Get<List<string>>("clientes").Should().BeNull("isolamento entre tenants no mesmo cache");
        }

        [Fact]
        public async Task GetOrCreateAsync_ExecutaFactoryUmaVezECacheia()
        {
            var cache = NovoCache();
            var ctx = MockTenant(1);
            var sut = new TenantAwareMemoryCache(cache, ctx.Object);
            var chamadas = 0;

            var v1 = await sut.GetOrCreateAsync("k", TimeSpan.FromMinutes(5),
                () => { chamadas++; return Task.FromResult<List<int>>(new() { 1, 2, 3 }); });
            var v2 = await sut.GetOrCreateAsync("k", TimeSpan.FromMinutes(5),
                () => { chamadas++; return Task.FromResult<List<int>>(new() { 9 }); });

            chamadas.Should().Be(1);
            v1.Should().BeEquivalentTo(v2);
        }

        [Fact]
        public void Global_NamespaceSeparado_NaoColideComTenants()
        {
            var cache = NovoCache();
            var ctx = MockTenant(1);
            var sut = new TenantAwareMemoryCache(cache, ctx.Object);

            sut.SetGlobal("config", new List<string> { "GLOBAL" }, TimeSpan.FromMinutes(5));
            sut.Set("config", new List<string> { "TENANT-1" }, TimeSpan.FromMinutes(5));

            sut.Get<List<string>>("config")![0].Should().Be("TENANT-1");
            sut.GetGlobal<List<string>>("config")![0].Should().Be("GLOBAL");
        }
    }
}

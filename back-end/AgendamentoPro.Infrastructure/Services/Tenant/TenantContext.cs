using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Infrastructure.Services.Tenant
{
    /// <summary>
    /// Implementação scoped do TenantContext, populado pelo TenantResolutionMiddleware.
    /// </summary>
    public class TenantContext : ITenantContext
    {
        public int? TenantId { get; private set; }
        public string Slug { get; private set; }
        public bool IsResolved => TenantId.HasValue;

        public void SetTenant(int tenantId, string slug)
        {
            TenantId = tenantId;
            Slug = slug;
        }
    }
}

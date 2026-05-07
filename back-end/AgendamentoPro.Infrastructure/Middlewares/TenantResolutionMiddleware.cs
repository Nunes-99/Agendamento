using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AgendamentoPro.Infrastructure.Middlewares
{
    /// <summary>
    /// Resolve o tenant atual a partir de (em ordem): claim do JWT, header X-Tenant-Slug
    /// ou primeiro segmento do path /api/t/{slug}/...
    /// </summary>
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next) { _next = next; }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantRepository tenants)
        {
            // 1) Claim
            var claimTenantId = context.User?.FindFirst("tenantId")?.Value;
            var claimSlug = context.User?.FindFirst("tenantSlug")?.Value;
            if (!string.IsNullOrWhiteSpace(claimTenantId) && int.TryParse(claimTenantId, out var tid))
            {
                tenantContext.SetTenant(tid, claimSlug);
            }

            // 2) Header
            if (!tenantContext.IsResolved && context.Request.Headers.TryGetValue("X-Tenant-Slug", out var slugHeader))
            {
                var t = await tenants.GetBySlugAsync(slugHeader.ToString());
                if (t != null) tenantContext.SetTenant(t.TenId, t.TenSlug);
            }

            // 3) Path /api/t/{slug}/...
            if (!tenantContext.IsResolved)
            {
                var path = context.Request.Path.Value ?? string.Empty;
                var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && parts[0].Equals("api", StringComparison.OrdinalIgnoreCase)
                    && parts[1].Equals("t", StringComparison.OrdinalIgnoreCase))
                {
                    var t = await tenants.GetBySlugAsync(parts[2]);
                    if (t != null) tenantContext.SetTenant(t.TenId, t.TenSlug);
                }
            }

            await _next(context);
        }
    }
}

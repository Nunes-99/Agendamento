using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    public abstract class BaseTenantController : ControllerBase
    {
        protected int RequireTenantId(ITenantContext ctx)
        {
            if (!ctx.IsResolved || !ctx.TenantId.HasValue)
                throw new UnauthorizedAccessException("Tenant não identificado.");
            return ctx.TenantId.Value;
        }
    }
}

using AgendamentoPro.Application.Interfaces.Dashboard;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Policy = "Atendente")]
    [Produces("application/json")]
    public class DashboardController : BaseTenantController
    {
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromServices] IDashboardUseCase useCase,
            [FromServices] ITenantContext ctx)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid));
        }
    }
}

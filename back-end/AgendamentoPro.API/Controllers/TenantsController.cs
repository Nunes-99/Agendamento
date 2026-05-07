using AgendamentoPro.Application.InputModels.Tenants;
using AgendamentoPro.Application.Interfaces.Tenants;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Route("api/tenants")]
    [Produces("application/json")]
    public class TenantsController : BaseTenantController
    {
        [HttpPost]
        [Authorize(Policy = "SuperAdmin")]
        public async Task<IActionResult> Criar([FromServices] ICriarTenantUseCase useCase,
            [FromBody] CriarTenantInputModel input)
        {
            var result = await useCase.ExecuteAsync(input);
            return CreatedAtAction(nameof(PorId), new { id = result.Id }, result);
        }

        [HttpGet]
        [Authorize(Policy = "SuperAdmin")]
        public async Task<IActionResult> Listar([FromServices] IConsultarTenantUseCase useCase)
            => Ok(await useCase.ListarTodosAsync());

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> PorId([FromServices] IConsultarTenantUseCase useCase, int id)
        {
            var t = await useCase.PorIdAsync(id);
            return t == null ? NotFound() : Ok(t);
        }

        // Público — usado pela landing page de cada empreendimento
        [HttpGet("public/by-slug/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> PorSlug([FromServices] IConsultarTenantUseCase useCase, string slug)
        {
            var t = await useCase.PorSlugAsync(slug);
            return t == null ? NotFound() : Ok(t);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Atualizar([FromServices] IAtualizarTenantUseCase useCase,
            int id, [FromBody] AtualizarTenantInputModel input)
            => Ok(await useCase.ExecuteAsync(id, input));

        [HttpPut("{id:int}/personalizacao")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Personalizacao([FromServices] IAtualizarTenantUseCase useCase,
            int id, [FromBody] AtualizarPersonalizacaoInputModel input)
            => Ok(await useCase.AtualizarPersonalizacaoAsync(id, input));

        [HttpPut("{id:int}/regras")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Regras([FromServices] IAtualizarTenantUseCase useCase,
            int id, [FromBody] AtualizarRegrasNegocioInputModel input)
            => Ok(await useCase.AtualizarRegrasAsync(id, input));
    }
}

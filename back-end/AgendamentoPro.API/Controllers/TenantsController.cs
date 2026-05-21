using AgendamentoPro.Application.InputModels.Tenants;
using AgendamentoPro.Application.Interfaces.Tenants;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database;
using AgendamentoPro.Infrastructure.Database.Multitenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/tenants")]
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
        public async Task<IActionResult> PorId([FromServices] IConsultarTenantUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            // CROSS-TENANT: SuperAdmin pode consultar qualquer tenant. Qualquer outro
            // usuário (admin tenant, atendente, cliente OTP) só vê o próprio. Sem essa
            // checagem, qualquer JWT válido lia email/CNPJ/telefone de outros tenants.
            if (!User.IsInRole("SuperAdmin"))
            {
                if (!ctx.IsResolved || ctx.TenantId != id)
                    return Forbid();
            }
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
            [FromServices] ITenantContext ctx, int id, [FromBody] AtualizarTenantInputModel input)
        {
            if (!ValidarOwnerOuSuperAdmin(ctx, id, out var forbid)) return forbid;
            return Ok(await useCase.ExecuteAsync(id, input));
        }

        [HttpPut("{id:int}/personalizacao")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Personalizacao([FromServices] IAtualizarTenantUseCase useCase,
            [FromServices] ITenantContext ctx, int id, [FromBody] AtualizarPersonalizacaoInputModel input)
        {
            if (!ValidarOwnerOuSuperAdmin(ctx, id, out var forbid)) return forbid;
            return Ok(await useCase.AtualizarPersonalizacaoAsync(id, input));
        }

        [HttpPut("{id:int}/regras")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Regras([FromServices] IAtualizarTenantUseCase useCase,
            [FromServices] ITenantContext ctx, int id, [FromBody] AtualizarRegrasNegocioInputModel input)
        {
            if (!ValidarOwnerOuSuperAdmin(ctx, id, out var forbid)) return forbid;
            return Ok(await useCase.AtualizarRegrasAsync(id, input));
        }

        /// <summary>
        /// Verifica que o admin chamando o endpoint é dono do tenant alvo (ou SuperAdmin).
        /// Sem isso, admin de A com JWT válido fazia PUT em /tenants/B/* e editava o B.
        /// </summary>
        private bool ValidarOwnerOuSuperAdmin(ITenantContext ctx, int tenantIdAlvo, out IActionResult forbid)
        {
            if (User.IsInRole("SuperAdmin")) { forbid = null; return true; }
            if (ctx.IsResolved && ctx.TenantId == tenantIdAlvo) { forbid = null; return true; }
            forbid = Forbid();
            return false;
        }

        /// <summary>
        /// Em modo PerTenant, inicializa o banco físico do tenant (cria arquivo .db
        /// e aplica migrations). No-op em modo Shared. Idempotente.
        /// </summary>
        [HttpPost("{id:int}/inicializar-database")]
        [Authorize(Policy = "SuperAdmin")]
        public async Task<IActionResult> InicializarDatabase(
            [FromServices] TenantDatabaseInitializer initializer,
            [FromServices] ITenantConnectionFactory factory,
            [FromServices] Core.Interfaces.Database.Repositories.ITenantRepository tenants,
            int id)
        {
            // Sem essa checagem, SuperAdmin com ID errado criava arquivo `tenant-X.db`
            // órfão (sem registro correspondente no banco shared). Acumulava lixo no
            // diretório TENANTS_PATH.
            var tenant = await tenants.GetByIdAsync(id);
            if (tenant == null) return NotFound(new { message = "Tenant não encontrado no banco compartilhado." });

            await initializer.EnsureDatabaseAsync(id);
            return Ok(new
            {
                tenantId = id,
                mode = factory.Mode,
                exists = factory.DatabaseExists(id)
            });
        }
    }
}

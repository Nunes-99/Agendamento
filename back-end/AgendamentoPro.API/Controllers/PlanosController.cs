using AgendamentoPro.Application.InputModels.Assinaturas;
using AgendamentoPro.Application.Interfaces.Assinaturas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Catálogo público de planos SaaS. Usado pela página de signup/upgrade
    /// e pela tela admin "Minha Assinatura" para listar opções.
    /// </summary>
    [ApiController]
    [Route("api/v1/planos")]
    [AllowAnonymous]
    [Produces("application/json")]
    public class PlanosController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Listar([FromServices] IListarPlanosUseCase useCase)
        {
            return Ok(await useCase.ExecuteAsync());
        }
    }

    /// <summary>
    /// CRUD do catálogo de planos. Apenas SuperAdmin (permite ajustar preço
    /// ou criar/desativar planos sem redeploy).
    /// </summary>
    [ApiController]
    [Route("api/v1/superadmin/planos")]
    [Authorize(Policy = "SuperAdmin")]
    [Produces("application/json")]
    public class SuperAdminPlanosController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Listar([FromServices] IListarTodosPlanosUseCase useCase)
            => Ok(await useCase.ExecuteAsync());

        [HttpPost]
        public async Task<IActionResult> Criar(
            [FromServices] ICriarPlanoUseCase useCase,
            [FromBody] PlanoCatalogoInputModel input)
            => Ok(await useCase.ExecuteAsync(input));

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Atualizar(
            [FromServices] IAtualizarPlanoUseCase useCase, int id,
            [FromBody] PlanoCatalogoInputModel input)
            => Ok(await useCase.ExecuteAsync(id, input));

        [HttpPost("{id:int}/ativar")]
        public async Task<IActionResult> Ativar(
            [FromServices] IAlternarStatusPlanoUseCase useCase, int id,
            [FromQuery] bool ativo = true)
            => Ok(await useCase.ExecuteAsync(id, ativo));
    }
}

using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Application.Interfaces.Agendamentos;
using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class AgendamentosController : BaseTenantController
    {
        // ----- Endpoints públicos (cliente final agenda) -----

        [HttpGet("api/v1/t/{slug}/slots")]
        [AllowAnonymous]
        public async Task<IActionResult> Slots(
            [FromServices] IConsultarSlotsUseCase useCase,
            [FromServices] ITenantContext ctx, string slug,
            [FromQuery] int servicoId, [FromQuery] DateTime data,
            [FromQuery] int? recursoId = null)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid, servicoId, data, recursoId));
        }

        [HttpPost("api/v1/t/{slug}/agendamentos")]
        [AllowAnonymous]
        public async Task<IActionResult> Criar(
            [FromServices] ICriarAgendamentoUseCase useCase,
            [FromServices] ITenantContext ctx, string slug,
            [FromBody] CriarAgendamentoInputModel input)
        {
            var tid = RequireTenantId(ctx);
            var result = await useCase.ExecuteAsync(tid, input);
            return Ok(result);
        }

        [HttpGet("api/v1/t/{slug}/agendamentos/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Status(
            [FromServices] IConsultarAgendamentoUseCase useCase,
            [FromServices] ITenantContext ctx, string slug, int id)
        {
            var tid = RequireTenantId(ctx);
            var ag = await useCase.PorIdAsync(tid, id);
            return ag == null ? NotFound() : Ok(ag);
        }

        [HttpGet("api/v1/t/{slug}/combos/grupos/{grupoComboId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> AgendamentosDoCombo(
            [FromServices] IConsultarAgendamentoUseCase useCase,
            [FromServices] ITenantContext ctx, string slug, Guid grupoComboId)
        {
            var tid = RequireTenantId(ctx);
            var lista = await useCase.PorGrupoComboAsync(tid, grupoComboId);
            return Ok(lista);
        }

        // ----- Endpoints administrativos -----

        [HttpGet("api/v1/admin/agendamentos")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Listar(
            [FromServices] IConsultarAgendamentoUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? data = null,
            [FromQuery] StatusAgendamento? status = null)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ListarPaginadoAsync(tid, page, pageSize, data, status));
        }

        [HttpGet("api/v1/admin/agendamentos/agenda")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Agenda(
            [FromServices] IConsultarAgendamentoUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromQuery] DateTime? data = null,
            [FromQuery] DateTime? inicio = null,
            [FromQuery] DateTime? fim = null,
            [FromQuery] int? recursoId = null)
        {
            var tid = RequireTenantId(ctx);
            if (inicio.HasValue && fim.HasValue)
                return Ok(await useCase.AgendaPorPeriodoAsync(tid, inicio.Value, fim.Value, recursoId));
            if (!data.HasValue)
                return BadRequest(new { message = "Informe 'data' ou 'inicio'+'fim'." });
            return Ok(await useCase.AgendaDoDiaAsync(tid, data.Value, recursoId));
        }

        [HttpPost("api/v1/admin/agendamentos")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> CriarAdmin(
            [FromServices] ICriarAgendamentoUseCase useCase,
            [FromServices] ITenantContext ctx,
            [FromBody] CriarAgendamentoAdminInputModel input)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAdminAsync(tid, input));
        }

        [HttpPost("api/v1/admin/agendamentos/{id:int}/reagendar")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Reagendar(
            [FromServices] IReagendarUseCase useCase,
            [FromServices] ITenantContext ctx, int id,
            [FromBody] ReagendarInputModel input)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid, id, input));
        }

        [HttpPost("api/v1/admin/agendamentos/{id:int}/cancelar")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Cancelar(
            [FromServices] ICancelarAgendamentoUseCase useCase,
            [FromServices] ITenantContext ctx, int id,
            [FromBody] CancelarAgendamentoInputModel input)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ExecuteAsync(tid, id, input));
        }

        [HttpPost("api/v1/admin/agendamentos/{id:int}/iniciar")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Iniciar(
            [FromServices] IAlterarStatusAgendamentoUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.IniciarAsync(tid, id));
        }

        [HttpPost("api/v1/admin/agendamentos/{id:int}/concluir")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> Concluir(
            [FromServices] IAlterarStatusAgendamentoUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ConcluirAsync(tid, id));
        }

        [HttpPost("api/v1/admin/agendamentos/{id:int}/no-show")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> NoShow(
            [FromServices] IAlterarStatusAgendamentoUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.NoShowAsync(tid, id));
        }

        [HttpPost("api/v1/admin/agendamentos/{id:int}/confirmar-pagamento")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> ConfirmarPagamento(
            [FromServices] IAlterarStatusAgendamentoUseCase useCase,
            [FromServices] ITenantContext ctx, int id)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await useCase.ConfirmarAsync(tid, id));
        }

        // ----- Fotos antes/depois -----

        [HttpPost("api/v1/admin/agendamentos/{id:int}/fotos")]
        [Authorize(Policy = "Atendente")]
        [RequestSizeLimit(15_000_000)]
        public async Task<IActionResult> UploadFoto(
            [FromServices] IFotoAgendamentoUseCase useCase, int id,
            [FromForm] TipoFoto tipo, IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                return BadRequest(new { message = "Arquivo obrigatório." });
            await using var stream = arquivo.OpenReadStream();
            var vm = await useCase.UploadAsync(id, tipo, arquivo.FileName, arquivo.ContentType, stream);
            return Ok(vm);
        }

        [HttpGet("api/v1/admin/agendamentos/{id:int}/fotos")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> ListarFotos(
            [FromServices] IFotoAgendamentoUseCase useCase, int id)
            => Ok(await useCase.ListarAsync(id));

        [HttpDelete("api/v1/admin/agendamentos/fotos/{fotoId:int}")]
        [Authorize(Policy = "Atendente")]
        public async Task<IActionResult> RemoverFoto(
            [FromServices] IFotoAgendamentoUseCase useCase, int fotoId)
        {
            await useCase.RemoverAsync(fotoId);
            return NoContent();
        }
    }
}

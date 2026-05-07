using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Application.Interfaces.Agendamentos;
using AgendamentoPro.Application.UseCases.Agendamentos;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Endpoints públicos para o cliente final gerenciar o próprio agendamento sem login.
    /// O acesso é via Guid `AgeAcessoToken` gerado na criação do agendamento e enviado
    /// ao cliente por WhatsApp/email no link "/t/{slug}/meu-agendamento/{token}".
    /// </summary>
    [ApiController]
    [Route("api/agendamentos/acesso")]
    [AllowAnonymous]
    [Produces("application/json")]
    public class MeuAgendamentoController : ControllerBase
    {
        [HttpGet("{token:guid}/fotos")]
        public async Task<IActionResult> Fotos(
            [FromServices] IAgendamentoRepository agendamentos,
            [FromServices] IFotoAgendamentoRepository fotos, Guid token)
        {
            var ag = await agendamentos.GetByAcessoTokenAsync(token);
            if (ag == null) return NotFound();
            var lista = await fotos.GetByAgendamentoAsync(ag.AgeId, ag.R_TenId);
            return Ok(lista.Select(f => new
            {
                id = f.FotId,
                tipo = f.FotTipo,
                url = f.FotUrl,
                criadoEm = f.FotCriadoEm
            }));
        }

        [HttpGet("{token:guid}")]
        public async Task<IActionResult> Obter(
            [FromServices] IAgendamentoRepository agendamentos, Guid token)
        {
            var ag = await agendamentos.GetByAcessoTokenAsync(token);
            if (ag == null) return NotFound();

            return Ok(new
            {
                id = ag.AgeId,
                tenantSlug = ag.Tenant?.TenSlug,
                clienteNome = ag.Cliente?.CliNome,
                servicoNome = ag.Servico?.SerNome,
                recursoNome = ag.Recurso?.RecNome,
                data = ag.AgeData,
                horaInicio = ag.AgeHoraInicio,
                horaFim = ag.AgeHoraFim,
                status = ag.AgeStatus.ToString(),
                statusPagamento = ag.AgePagamentoStatus.ToString(),
                valorTotal = ag.AgeValorTotal,
                podeReagendar = ag.AgeStatus == Core.Enums.StatusAgendamento.Confirmado
                    || ag.AgeStatus == Core.Enums.StatusAgendamento.PendentePagamento,
                podeCancelar = ag.AgeStatus != Core.Enums.StatusAgendamento.Concluido
                    && ag.AgeStatus != Core.Enums.StatusAgendamento.Cancelado,
                ehCombo = ag.AgeGrupoComboId.HasValue
            });
        }

        [HttpPost("{token:guid}/cancelar")]
        public async Task<IActionResult> Cancelar(
            [FromServices] IAgendamentoRepository agendamentos,
            [FromServices] ITenantRepository tenants,
            [FromServices] IUnitOfWork uow,
            Guid token, [FromBody] CancelarAgendamentoInputModel input)
        {
            var ag = await agendamentos.GetByAcessoTokenAsync(token);
            if (ag == null) return NotFound();

            // Regra: cliente só cancela com antecedência ≥ TenLimiteCancelamentoHoras
            var tenant = await tenants.GetByIdAsync(ag.R_TenId);
            var antecedencia = ag.DataHoraInicio - DateTime.Now;
            if (tenant != null && antecedencia.TotalHours < tenant.TenLimiteCancelamentoHoras)
            {
                return BadRequest(new
                {
                    message = $"Cancelamento exige antecedência de {tenant.TenLimiteCancelamentoHoras}h. Entre em contato com o estabelecimento."
                });
            }

            var motivo = string.IsNullOrWhiteSpace(input?.Motivo)
                ? "Cancelado pelo cliente."
                : input.Motivo;

            // Combo: cancela todos do grupo
            if (ag.AgeGrupoComboId.HasValue)
            {
                var grupo = await agendamentos.GetByGrupoComboAsync(ag.AgeGrupoComboId.Value);
                foreach (var item in grupo.Where(g => g.R_TenId == ag.R_TenId
                    && g.AgeStatus != Core.Enums.StatusAgendamento.Cancelado
                    && g.AgeStatus != Core.Enums.StatusAgendamento.Concluido))
                {
                    item.Cancelar(motivo);
                    await agendamentos.UpdateAsync(item);
                }
            }
            else
            {
                ag.Cancelar(motivo);
                await agendamentos.UpdateAsync(ag);
            }
            await uow.SaveChangesAsync();
            return Ok(new { sucesso = true });
        }

        [HttpPost("{token:guid}/reagendar")]
        public async Task<IActionResult> Reagendar(
            [FromServices] IAgendamentoRepository agendamentos,
            [FromServices] IServicoRepository servicos,
            [FromServices] ITenantRepository tenants,
            [FromServices] IUnitOfWork uow,
            Guid token, [FromBody] ReagendarInputModel input)
        {
            var ag = await agendamentos.GetByAcessoTokenAsync(token);
            if (ag == null) return NotFound();

            if (ag.AgeGrupoComboId.HasValue)
                return BadRequest(new
                {
                    message = "Combos não podem ser reagendados individualmente. Cancele e crie um novo."
                });

            var tenant = await tenants.GetByIdAsync(ag.R_TenId);
            var antecedencia = ag.DataHoraInicio - DateTime.Now;
            if (tenant != null && antecedencia.TotalHours < tenant.TenLimiteCancelamentoHoras)
                return BadRequest(new
                {
                    message = $"Reagendamento exige antecedência de {tenant.TenLimiteCancelamentoHoras}h."
                });

            var novaDataHora = input.NovaData.Date.Add(input.NovaHoraInicio);
            if (tenant != null && novaDataHora < DateTime.Now.AddHours(tenant.TenAntecedenciaMinHoras))
                return BadRequest(new
                {
                    message = $"Nova data deve respeitar antecedência mínima de {tenant.TenAntecedenciaMinHoras}h."
                });

            var servico = await servicos.GetByIdAsync(ag.R_SerId, ag.R_TenId);
            if (servico == null) return BadRequest(new { message = "Serviço inválido." });

            var horaFim = input.NovaHoraInicio.Add(TimeSpan.FromMinutes(servico.SerDuracaoMinutos));
            var buffer = TimeSpan.FromMinutes(tenant?.TenBufferMinutos ?? 0);

            if (await agendamentos.ExisteConflitoAsync(ag.R_TenId, ag.R_RecId, input.NovaData.Date,
                    input.NovaHoraInicio.Subtract(buffer), horaFim.Add(buffer), ag.AgeId))
            {
                return BadRequest(new { message = "Horário indisponível." });
            }

            try
            {
                ag.Reagendar(input.NovaData, input.NovaHoraInicio, horaFim);
                await agendamentos.UpdateAsync(ag);
                await uow.SaveChangesAsync();
                return Ok(new { sucesso = true, novaData = ag.AgeData, novaHora = ag.AgeHoraInicio });
            }
            catch (AgendamentoException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

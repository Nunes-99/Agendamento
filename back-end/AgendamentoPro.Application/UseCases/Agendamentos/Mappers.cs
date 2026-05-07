using AgendamentoPro.Application.ViewModels.Agendamentos;
using AgendamentoPro.Core.Entities.Agendamentos;

namespace AgendamentoPro.Application.UseCases.Agendamentos
{
    internal static class AgendamentoMapper
    {
        public static AgendamentoViewModel Map(Agendamento a) => new()
        {
            Id = a.AgeId,
            TenantId = a.R_TenId,
            ClienteId = a.R_CliId,
            ClienteNome = a.Cliente?.CliNome,
            ClienteTelefone = a.Cliente?.CliTelefone ?? a.Cliente?.CliWhatsApp,
            ServicoId = a.R_SerId,
            ServicoNome = a.Servico?.SerNome,
            RecursoId = a.R_RecId,
            RecursoNome = a.Recurso?.RecNome,
            Data = a.AgeData,
            HoraInicio = a.AgeHoraInicio,
            HoraFim = a.AgeHoraFim,
            Status = a.AgeStatus,
            StatusDescricao = a.AgeStatus.ToString(),
            StatusPagamento = a.AgePagamentoStatus,
            ValorTotal = a.AgeValorTotal,
            ValorEntrada = a.AgeValorEntrada,
            Observacao = a.AgeObservacao,
            MotivoCancelamento = a.AgeMotivoCancelamento,
            CriadoEm = a.AgeCriadoEm
        };
    }
}

using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Application.ViewModels.Agendamentos;
using AgendamentoPro.Application.ViewModels.Common;
using AgendamentoPro.Core.Enums;

namespace AgendamentoPro.Application.Interfaces.Agendamentos
{
    public interface ICriarAgendamentoUseCase
    {
        Task<CriarAgendamentoResultViewModel> ExecuteAsync(int tenantId, CriarAgendamentoInputModel input);
        Task<AgendamentoViewModel> ExecuteAdminAsync(int tenantId, CriarAgendamentoAdminInputModel input);
    }
    public interface IConsultarSlotsUseCase
    {
        Task<IEnumerable<SlotDisponivelViewModel>> ExecuteAsync(int tenantId, int servicoId, DateTime data, int? recursoId = null);

        /// <summary>
        /// Quantas vagas cada dia do período tem. O cliente precisa disso para
        /// escolher a data sabendo onde há vaga, em vez de tentar dia a dia.
        /// </summary>
        Task<IEnumerable<DiaDisponivelViewModel>> DiasAsync(int tenantId, int servicoId,
            DateTime inicio, int dias, int? recursoId = null);
    }
    public interface IConsultarAgendamentoUseCase
    {
        Task<AgendamentoViewModel> PorIdAsync(int tenantId, int id);
        Task<IEnumerable<AgendamentoViewModel>> AgendaDoDiaAsync(int tenantId, DateTime data, int? recursoId);
        Task<IEnumerable<AgendamentoViewModel>> AgendaPorPeriodoAsync(int tenantId, DateTime inicio, DateTime fim, int? recursoId);
        Task<PaginadoViewModel<AgendamentoViewModel>> ListarPaginadoAsync(int tenantId, int page, int pageSize, DateTime? data, StatusAgendamento? status);
        Task<IEnumerable<AgendamentoViewModel>> PorGrupoComboAsync(int tenantId, Guid grupoComboId);

        /// <summary>
        /// Cobrança em aberto do agendamento (QR do PIX / link do cartão).
        /// O QR só existia no history.state da navegação: bastava o cliente
        /// atualizar a página de pagamento para perdê-lo e não ter como pagar.
        /// </summary>
        Task<PagamentoViewModel> CobrancaEmAbertoAsync(int tenantId, int agendamentoId);
    }
    public interface IReagendarUseCase
    {
        Task<AgendamentoViewModel> ExecuteAsync(int tenantId, int id, ReagendarInputModel input);
    }
    public interface ICancelarAgendamentoUseCase
    {
        Task<AgendamentoViewModel> ExecuteAsync(int tenantId, int id, CancelarAgendamentoInputModel input);
    }
    public interface IAlterarStatusAgendamentoUseCase
    {
        Task<AgendamentoViewModel> ConfirmarAsync(int tenantId, int id);
        Task<AgendamentoViewModel> IniciarAsync(int tenantId, int id);
        Task<AgendamentoViewModel> ConcluirAsync(int tenantId, int id);
        Task<AgendamentoViewModel> NoShowAsync(int tenantId, int id);
    }
}

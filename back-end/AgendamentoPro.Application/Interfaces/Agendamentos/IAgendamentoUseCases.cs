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
    }
    public interface IConsultarAgendamentoUseCase
    {
        Task<AgendamentoViewModel> PorIdAsync(int tenantId, int id);
        Task<IEnumerable<AgendamentoViewModel>> AgendaDoDiaAsync(int tenantId, DateTime data, int? recursoId);
        Task<IEnumerable<AgendamentoViewModel>> AgendaPorPeriodoAsync(int tenantId, DateTime inicio, DateTime fim, int? recursoId);
        Task<PaginadoViewModel<AgendamentoViewModel>> ListarPaginadoAsync(int tenantId, int page, int pageSize, DateTime? data, StatusAgendamento? status);
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

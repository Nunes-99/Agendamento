using AgendamentoPro.Application.InputModels.Recursos;
using AgendamentoPro.Application.ViewModels.Recursos;

namespace AgendamentoPro.Application.Interfaces.Recursos
{
    public interface ICadastrarRecursoUseCase
    {
        Task<RecursoViewModel> ExecuteAsync(int tenantId, RecursoInputModel input);
    }
    public interface IAtualizarRecursoUseCase
    {
        Task<RecursoViewModel> ExecuteAsync(int tenantId, int id, RecursoInputModel input);
    }
    public interface IConsultarRecursoUseCase
    {
        Task<RecursoViewModel> PorIdAsync(int tenantId, int id);
        Task<IEnumerable<RecursoViewModel>> ListarAsync(int tenantId, bool somenteAtivos);
    }
    public interface IInativarRecursoUseCase
    {
        Task ExecuteAsync(int tenantId, int id);
    }
}

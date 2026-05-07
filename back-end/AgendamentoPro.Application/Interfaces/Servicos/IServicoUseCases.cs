using AgendamentoPro.Application.InputModels.Servicos;
using AgendamentoPro.Application.ViewModels.Servicos;

namespace AgendamentoPro.Application.Interfaces.Servicos
{
    public interface ICadastrarServicoUseCase
    {
        Task<ServicoViewModel> ExecuteAsync(int tenantId, ServicoInputModel input);
    }

    public interface IAtualizarServicoUseCase
    {
        Task<ServicoViewModel> ExecuteAsync(int tenantId, int id, ServicoInputModel input);
    }

    public interface IConsultarServicoUseCase
    {
        Task<ServicoViewModel> PorIdAsync(int tenantId, int id);
        Task<IEnumerable<ServicoViewModel>> ListarAsync(int tenantId, bool somenteAtivos);
    }

    public interface IInativarServicoUseCase
    {
        Task ExecuteAsync(int tenantId, int id);
    }
}

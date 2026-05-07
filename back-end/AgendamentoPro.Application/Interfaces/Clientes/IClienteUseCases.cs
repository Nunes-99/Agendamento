using AgendamentoPro.Application.InputModels.Clientes;
using AgendamentoPro.Application.ViewModels.Clientes;
using AgendamentoPro.Application.ViewModels.Common;

namespace AgendamentoPro.Application.Interfaces.Clientes
{
    public interface ICadastrarClienteUseCase
    {
        Task<ClienteViewModel> ExecuteAsync(int tenantId, ClienteInputModel input);
    }
    public interface IAtualizarClienteUseCase
    {
        Task<ClienteViewModel> ExecuteAsync(int tenantId, int id, ClienteInputModel input);
    }
    public interface IConsultarClienteUseCase
    {
        Task<ClienteViewModel> PorIdAsync(int tenantId, int id);
        Task<PaginadoViewModel<ClienteViewModel>> ListarPaginadoAsync(int tenantId, int page, int pageSize, string busca);
    }
}

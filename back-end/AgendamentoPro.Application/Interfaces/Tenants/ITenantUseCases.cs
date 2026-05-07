using AgendamentoPro.Application.InputModels.Tenants;
using AgendamentoPro.Application.ViewModels.Tenants;

namespace AgendamentoPro.Application.Interfaces.Tenants
{
    public interface ICriarTenantUseCase
    {
        Task<TenantViewModel> ExecuteAsync(CriarTenantInputModel input);
    }

    public interface IConsultarTenantUseCase
    {
        Task<TenantViewModel> PorIdAsync(int id);
        Task<TenantViewModel> PorSlugAsync(string slug);
        Task<IEnumerable<TenantViewModel>> ListarTodosAsync();
    }

    public interface IAtualizarTenantUseCase
    {
        Task<TenantViewModel> ExecuteAsync(int id, AtualizarTenantInputModel input);
        Task<TenantViewModel> AtualizarPersonalizacaoAsync(int id, AtualizarPersonalizacaoInputModel input);
        Task<TenantViewModel> AtualizarRegrasAsync(int id, AtualizarRegrasNegocioInputModel input);
    }
}

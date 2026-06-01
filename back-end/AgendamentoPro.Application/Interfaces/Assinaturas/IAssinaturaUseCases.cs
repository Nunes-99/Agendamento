using AgendamentoPro.Application.InputModels.Assinaturas;
using AgendamentoPro.Application.ViewModels.Assinaturas;

namespace AgendamentoPro.Application.Interfaces.Assinaturas
{
    public interface IListarPlanosUseCase
    {
        Task<IEnumerable<PlanoViewModel>> ExecuteAsync();
    }

    public interface IMinhaAssinaturaUseCase
    {
        Task<AssinaturaViewModel> ExecuteAsync(int tenantId);
    }

    public interface ICriarAssinaturaUseCase
    {
        Task<AssinaturaViewModel> ExecuteAsync(int tenantId, CriarAssinaturaInputModel input);
    }

    public interface IAlterarPlanoUseCase
    {
        Task<AssinaturaViewModel> ExecuteAsync(int tenantId, AlterarPlanoInputModel input);
    }

    public interface ICancelarAssinaturaUseCase
    {
        Task<AssinaturaViewModel> ExecuteAsync(int tenantId);
    }

    public interface IProcessarWebhookAssinaturaUseCase
    {
        Task ExecuteAsync(string gateway, string payload, string assinatura);
    }

    // SuperAdmin — gestão do catálogo de planos.
    public interface IListarTodosPlanosUseCase
    {
        Task<IEnumerable<PlanoViewModel>> ExecuteAsync();
    }

    public interface ICriarPlanoUseCase
    {
        Task<PlanoViewModel> ExecuteAsync(PlanoCatalogoInputModel input);
    }

    public interface IAtualizarPlanoUseCase
    {
        Task<PlanoViewModel> ExecuteAsync(int planoId, PlanoCatalogoInputModel input);
    }

    public interface IAlternarStatusPlanoUseCase
    {
        Task<PlanoViewModel> ExecuteAsync(int planoId, bool ativo);
    }
}

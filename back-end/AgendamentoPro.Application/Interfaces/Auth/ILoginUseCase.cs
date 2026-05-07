using AgendamentoPro.Application.InputModels.Auth;
using AgendamentoPro.Application.ViewModels.Auth;

namespace AgendamentoPro.Application.Interfaces.Auth
{
    public interface ILoginUseCase
    {
        Task<LoginViewModel> ExecuteAsync(LoginInputModel input);
    }

    public interface IRefreshTokenUseCase
    {
        Task<LoginViewModel> ExecuteAsync(RefreshTokenInputModel input);
    }
}

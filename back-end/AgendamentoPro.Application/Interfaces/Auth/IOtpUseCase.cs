using AgendamentoPro.Application.InputModels.Auth;
using AgendamentoPro.Application.ViewModels.Auth;

namespace AgendamentoPro.Application.Interfaces.Auth
{
    public interface IOtpUseCase
    {
        Task<SolicitarOtpResultViewModel> SolicitarAsync(int tenantId, string slugTenant, SolicitarOtpInputModel input);
        Task<ValidarOtpResultViewModel> ValidarAsync(int tenantId, string slugTenant, ValidarOtpInputModel input);
    }
}

using AgendamentoPro.Application.ViewModels.Dashboard;

namespace AgendamentoPro.Application.Interfaces.Dashboard
{
    public interface IDashboardUseCase
    {
        Task<DashboardViewModel> ExecuteAsync(int tenantId);
    }
}

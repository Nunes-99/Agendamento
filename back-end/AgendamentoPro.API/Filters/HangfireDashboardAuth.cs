using Hangfire.Dashboard;

namespace AgendamentoPro.API.Filters
{
    /// <summary>
    /// Autorização do dashboard /hangfire.
    /// Apenas SuperAdmin acessa — jobs são globais (não por tenant), e Administrador
    /// de tenant não deve ter visão dos jobs dos outros. Para visão restrita por tenant,
    /// implemente um relatório dedicado em /admin/jobs.
    /// </summary>
    public class HangfireDashboardAuth : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var http = context.GetHttpContext();
            if (http?.User?.Identity?.IsAuthenticated != true) return false;
            // Restringido a SuperAdmin: tenant-admin ver jobs de outros é vazamento.
            return http.User.IsInRole("SuperAdmin");
        }
    }
}

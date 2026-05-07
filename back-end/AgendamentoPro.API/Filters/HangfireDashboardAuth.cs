using Hangfire.Dashboard;

namespace AgendamentoPro.API.Filters
{
    /// <summary>
    /// Autorização do dashboard /hangfire: exige usuário autenticado com role
    /// SuperAdmin ou Administrador. Bloqueia acesso anônimo (default do Hangfire
    /// libera só localhost — em prod isso não basta).
    /// </summary>
    public class HangfireDashboardAuth : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var http = context.GetHttpContext();
            if (http?.User?.Identity?.IsAuthenticated != true) return false;
            return http.User.IsInRole("SuperAdmin") || http.User.IsInRole("Administrador");
        }
    }
}

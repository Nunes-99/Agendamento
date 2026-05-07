using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Security.Claims;

namespace AgendamentoPro.Infrastructure.Middlewares
{
    /// <summary>
    /// Adiciona propriedades de contexto (TenantId, TenantSlug, UserId, CorrelationId)
    /// ao Serilog LogContext durante o ciclo da request — todos os logs emitidos
    /// dentro do request herdam essas propriedades automaticamente.
    /// Deve ser registrado APÓS UseAuthentication e UseTenantResolution.
    /// </summary>
    public class LogEnrichmentMiddleware
    {
        private const string CorrelationHeader = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        public LogEnrichmentMiddleware(RequestDelegate next) { _next = next; }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
        {
            var correlationId = context.Request.Headers.TryGetValue(CorrelationHeader, out var headerValue)
                && !string.IsNullOrWhiteSpace(headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString("N");

            context.Response.Headers[CorrelationHeader] = correlationId;

            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User?.FindFirst("sub")?.Value;
            var userEmail = context.User?.FindFirst(ClaimTypes.Email)?.Value;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("TenantId", tenantContext.TenantId?.ToString() ?? "-"))
            using (LogContext.PushProperty("TenantSlug", tenantContext.Slug ?? "-"))
            using (LogContext.PushProperty("UserId", userId ?? "-"))
            using (LogContext.PushProperty("UserEmail", userEmail ?? "-"))
            {
                await _next(context);
            }
        }
    }

    public static class LogEnrichmentMiddlewareExtensions
    {
        public static IApplicationBuilder UseLogEnrichment(this IApplicationBuilder app)
            => app.UseMiddleware<LogEnrichmentMiddleware>();
    }
}

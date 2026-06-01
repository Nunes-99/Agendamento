using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Infrastructure.Services.Assinaturas;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AgendamentoPro.Infrastructure.Middlewares
{
    /// <summary>
    /// Bloqueia operações em tenants inadimplentes:
    /// - Status ReadOnly/Expirada + endpoint admin de escrita (POST/PUT/DELETE/PATCH) → 402 Payment Required.
    /// - Status ReadOnly/Expirada/Cancelada + qualquer endpoint da área pública do tenant (/api/v1/t/{slug}/) → 503.
    /// - Whitelist (sempre permitido): billing, planos, webhooks, auth, health, hubs SignalR, hangfire.
    ///
    /// Deve ser registrado APÓS UseTenantResolution (para ITenantContext) e APÓS UseAuthentication.
    /// Usa cache em memória (30s TTL) — invalidação via IAssinaturaCacheInvalidator dos use cases.
    /// </summary>
    public class AssinaturaGuardMiddleware
    {
        private readonly RequestDelegate _next;

        public AssinaturaGuardMiddleware(RequestDelegate next) { _next = next; }

        public async Task InvokeAsync(HttpContext ctx, ITenantContext tenantContext,
            IAssinaturaRepository assinaturas, AssinaturaStatusCache cache)
        {
            if (!tenantContext.IsResolved || !tenantContext.TenantId.HasValue)
            {
                await _next(ctx);
                return;
            }

            var path = ctx.Request.Path.Value ?? string.Empty;
            if (EhWhitelisted(path))
            {
                await _next(ctx);
                return;
            }

            var status = await cache.ObterStatusAsync(tenantContext.TenantId.Value, assinaturas);

            // Sem assinatura: deixa passar (tenant novo precisa criar uma).
            if (!status.HasValue)
            {
                await _next(ctx);
                return;
            }

            // Status saudáveis (com escrita permitida): passa.
            if (status is StatusAssinatura.Trial
                       or StatusAssinatura.Ativa
                       or StatusAssinatura.Atrasada)
            {
                await _next(ctx);
                return;
            }

            // ReadOnly / Cancelada / Expirada: bloqueio diferenciado por área.
            if (EhPublicoDoTenant(path))
            {
                ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                ctx.Response.ContentType = "application/problem+json";
                await ctx.Response.WriteAsync(@"{""type"":""urn:agendamentopro:assinatura:tenant-suspenso"","
                    + @"""title"":""Serviço temporariamente indisponível"","
                    + @"""status"":503,"
                    + @"""detail"":""Esta empresa está com a assinatura pendente. Tente novamente mais tarde.""}");
                return;
            }

            // Admin: leitura permitida em ReadOnly (consultar dados + acessar billing).
            var metodo = ctx.Request.Method;
            var ehEscrita = metodo == HttpMethods.Post || metodo == HttpMethods.Put
                         || metodo == HttpMethods.Delete || metodo == HttpMethods.Patch;
            if (!ehEscrita)
            {
                await _next(ctx);
                return;
            }

            ctx.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsync(@"{""type"":""urn:agendamentopro:assinatura:pagamento-requerido"","
                + @"""title"":""Pagamento da assinatura requerido"","
                + @"""status"":402,"
                + $@"""detail"":""Assinatura em status {status}. Regularize em /admin/minha-assinatura para reativar.""}}");
        }

        private static bool EhPublicoDoTenant(string path)
        {
            var p = path.TrimStart('/');
            return p.StartsWith("api/v1/t/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("api/t/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EhWhitelisted(string path)
        {
            var p = path.TrimStart('/');
            return p.StartsWith("api/v1/admin/assinatura", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("api/v1/planos", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("api/v1/superadmin/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("api/v1/webhooks/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("api/v1/auth/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("api/auth/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("api/health/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("hubs/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("hangfire", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class AssinaturaGuardMiddlewareExtensions
    {
        public static IApplicationBuilder UseAssinaturaGuard(this IApplicationBuilder app)
            => app.UseMiddleware<AssinaturaGuardMiddleware>();
    }
}

using AgendamentoPro.Core.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace AgendamentoPro.Infrastructure.Middlewares
{
    /// <summary>
    /// Captura exceções não tratadas e devolve ProblemDetails (RFC 7807) com traceId
    /// pra correlacionar logs e respostas. Mantém backwards-compat retornando "message"
    /// no body também (frontend já consome esse campo).
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try { await _next(context); }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain error: {Message}", ex.Message);
                await EscreverProblemAsync(context, HttpStatusCode.BadRequest,
                    title: "Erro de domínio", detail: ex.Message, type: "domain-error");
            }
            catch (UnauthorizedAccessException ex)
            {
                // 403 Forbidden: usuário autenticado mas sem permissão/escopo (ex: SuperAdmin sem tenant
                // tentando endpoint tenant-scoped). 401 fica reservado para token inválido/expirado.
                await EscreverProblemAsync(context, HttpStatusCode.Forbidden,
                    title: "Acesso negado", detail: ex.Message, type: "forbidden");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro não tratado");
                await EscreverProblemAsync(context, HttpStatusCode.InternalServerError,
                    title: "Erro interno", detail: "Erro interno do servidor.", type: "internal-error");
            }
        }

        private static async Task EscreverProblemAsync(HttpContext context, HttpStatusCode status,
            string title, string detail, string type)
        {
            if (context.Response.HasStarted) return;
            context.Response.Clear();
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)status;

            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            var problem = new
            {
                type = $"https://agendamentopro/errors/{type}",
                title,
                status = (int)status,
                detail,
                instance = context.Request.Path.Value,
                traceId,
                // Backwards-compat: o frontend ainda lê `message` em vários lugares.
                message = detail
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOpts));
        }
    }

    /// <summary>
    /// Garante um Correlation-Id por request: lê do header X-Correlation-Id se vier,
    /// senão gera um novo. Anexa ao response e ao Activity (logs estruturados).
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string Header = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) { _next = next; }

        public async Task InvokeAsync(HttpContext context)
        {
            string corr;
            if (context.Request.Headers.TryGetValue(Header, out var v) && !string.IsNullOrWhiteSpace(v))
                corr = v.ToString();
            else
                corr = Guid.NewGuid().ToString("N");

            Activity.Current?.SetTag("correlationId", corr);
            context.Items["CorrelationId"] = corr;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[Header] = corr;
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

    public static class ErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder app)
            => app.UseMiddleware<ErrorHandlingMiddleware>();

        public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app)
            => app.UseMiddleware<TenantResolutionMiddleware>();

        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
            => app.UseMiddleware<CorrelationIdMiddleware>();
    }
}

using AgendamentoPro.Core.Entities.Common;
using AgendamentoPro.Core.Interfaces.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework
{
    /// <summary>
    /// Captura alterações (INSERT/UPDATE/DELETE) em entidades configuradas e grava
    /// um registro em LogAuditoria. Roda como SaveChangesInterceptor — transparente
    /// para use cases.
    ///
    /// Decisões:
    /// - Sempre mascara propriedades cujo nome contém "senha", "password", "token",
    ///   "secret" — não vaza credenciais no JSON do log.
    /// - LogAuditoria em si é IGNORADO (anti loop).
    /// - Se HttpContext indisponível (jobs em background), grava sem user/ip.
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        // Campos com dados sensíveis (senhas/tokens/secrets/CPF). NÃO inclui
        // PagPayloadGateway nem WhEvPayload — esses são JSONs dos gateways úteis
        // pra debug e não contêm credenciais (apenas valores e IDs de transação
        // que já estão noutros campos).
        //
        // CPF é PII forte sob LGPD: trata como sensível. Telefone/email/nome ficam
        // legíveis para diagnóstico de incidentes (audit purge limita a 12 meses).
        private static readonly HashSet<string> CamposSensiveis = new(StringComparer.OrdinalIgnoreCase)
        {
            "UsuSenha", "RpsToken", "RefToken", "UsuTotpSecret", "CliCpf"
        };

        private readonly IHttpContextAccessor _http;

        public AuditInterceptor(IHttpContextAccessor http) { _http = http; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not DbContext ctx) return base.SavingChangesAsync(eventData, result, cancellationToken);

            var entries = ctx.ChangeTracker.Entries()
                .Where(e => e.Entity is not LogAuditoria
                    && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            if (entries.Count == 0) return base.SavingChangesAsync(eventData, result, cancellationToken);

            var http = _http.HttpContext;
            int? tenantId = null;
            int? usuarioId = null;
            string usuarioEmail = null;
            string ip = null;
            string correlationId = null;

            if (http != null)
            {
                ip = http.Connection.RemoteIpAddress?.ToString();
                correlationId = http.Response.Headers["X-Correlation-Id"].ToString();
                if (string.IsNullOrEmpty(correlationId))
                    correlationId = http.Request.Headers["X-Correlation-Id"].ToString();

                var idStr = http.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? http.User?.FindFirst("sub")?.Value;
                if (int.TryParse(idStr, out var uid)) usuarioId = uid;
                usuarioEmail = http.User?.FindFirst(ClaimTypes.Email)?.Value;

                var tCtx = http.RequestServices?.GetService(typeof(ITenantContext)) as ITenantContext;
                tenantId = tCtx?.TenantId;
            }

            var logs = new List<LogAuditoria>();
            foreach (var entry in entries)
            {
                var tabela = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
                var chave = ExtrairChave(entry);
                var acao = entry.State switch
                {
                    EntityState.Added => "Insert",
                    EntityState.Modified => "Update",
                    EntityState.Deleted => "Delete",
                    _ => entry.State.ToString()
                };

                var antes = entry.State == EntityState.Added ? null : Serializar(entry, original: true);
                var depois = entry.State == EntityState.Deleted ? null : Serializar(entry, original: false);

                logs.Add(new LogAuditoria(tenantId, usuarioId, usuarioEmail, ip, correlationId,
                    tabela, chave, acao, antes, depois));
            }

            if (logs.Count > 0)
            {
                ctx.AddRange(logs);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static string ExtrairChave(EntityEntry entry)
        {
            var pk = entry.Metadata.FindPrimaryKey();
            if (pk == null) return string.Empty;
            var valores = pk.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "")
                .ToArray();
            return string.Join(",", valores);
        }

        private static string Serializar(EntityEntry entry, bool original)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.IsShadowProperty()) continue;
                var nome = prop.Metadata.Name;
                var valor = original
                    ? (entry.State == EntityState.Added ? null : prop.OriginalValue)
                    : prop.CurrentValue;
                if (CamposSensiveis.Contains(nome) || nome.Contains("Senha", StringComparison.OrdinalIgnoreCase)
                    || nome.Contains("Password", StringComparison.OrdinalIgnoreCase)
                    || nome.Contains("Secret", StringComparison.OrdinalIgnoreCase)
                    || nome.Contains("Cpf", StringComparison.OrdinalIgnoreCase))
                {
                    dict[nome] = valor == null ? null : "***";
                }
                else
                {
                    dict[nome] = valor;
                }
            }
            try
            {
                var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                // Limita a 8KB pra alinhar com MaxLength da coluna; entidades enormes
                // ficam truncadas com sufixo claro.
                return json.Length > 8000 ? json[..7980] + "...[trunc]" : json;
            }
            catch
            {
                return "{\"_erro\":\"falha ao serializar\"}";
            }
        }
    }
}

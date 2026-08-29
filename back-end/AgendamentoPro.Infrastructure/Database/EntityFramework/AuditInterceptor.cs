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

        // INSERTs pendentes por contexto: antes do save a entidade Added ainda tem a
        // chave TEMPORÁRIA do EF (negativa) — gravar nesse momento produzia logs como
        // "Agendamento #-2147482644", impossíveis de correlacionar com a linha real.
        // Por isso o log de Insert é criado em SavedChangesAsync, com o ID definitivo.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DbContext, List<EntityEntry>>
            _insercoesPendentes = new();

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not DbContext ctx) return base.SavingChangesAsync(eventData, result, cancellationToken);

            var entries = ctx.ChangeTracker.Entries()
                .Where(e => e.Entity is not LogAuditoria
                    && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            if (entries.Count == 0) return base.SavingChangesAsync(eventData, result, cancellationToken);

            var (tenantId, usuarioId, usuarioEmail, ip, correlationId) = CapturarContextoHttp();

            var logs = new List<LogAuditoria>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    var pendentes = _insercoesPendentes.GetOrCreateValue(ctx);
                    pendentes.Add(entry);
                    continue;
                }

                var tabela = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
                var chave = ExtrairChave(entry);
                var acao = entry.State == EntityState.Deleted ? "Delete" : "Update";

                var antes = Serializar(entry, original: true);
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

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,
            int result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is DbContext ctx
                && _insercoesPendentes.TryGetValue(ctx, out var pendentes)
                && pendentes.Count > 0)
            {
                // Remove antes de salvar: o SaveChanges aninhado reentra no interceptor
                // e a lista vazia garante que não há loop.
                _insercoesPendentes.Remove(ctx);

                var (tenantId, usuarioId, usuarioEmail, ip, correlationId) = CapturarContextoHttp();
                var logs = new List<LogAuditoria>();
                foreach (var entry in pendentes)
                {
                    var tabela = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
                    logs.Add(new LogAuditoria(tenantId, usuarioId, usuarioEmail, ip, correlationId,
                        tabela, ExtrairChave(entry), "Insert",
                        null, Serializar(entry, original: false)));
                }
                ctx.AddRange(logs);
                await ctx.SaveChangesAsync(cancellationToken);
            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is DbContext ctx) _insercoesPendentes.Remove(ctx);
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        private (int? tenantId, int? usuarioId, string email, string ip, string correlationId) CapturarContextoHttp()
        {
            var http = _http.HttpContext;
            if (http == null) return (null, null, null, null, null);

            var ip = http.Connection.RemoteIpAddress?.ToString();
            var correlationId = http.Response.Headers["X-Correlation-Id"].ToString();
            if (string.IsNullOrEmpty(correlationId))
                correlationId = http.Request.Headers["X-Correlation-Id"].ToString();

            int? usuarioId = null;
            var idStr = http.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? http.User?.FindFirst("sub")?.Value;
            if (int.TryParse(idStr, out var uid)) usuarioId = uid;
            var usuarioEmail = http.User?.FindFirst(ClaimTypes.Email)?.Value;

            var tCtx = http.RequestServices?.GetService(typeof(ITenantContext)) as ITenantContext;
            return (tCtx?.TenantId, usuarioId, usuarioEmail, ip, correlationId);
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

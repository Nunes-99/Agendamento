using AgendamentoPro.API.Filters;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using AgendamentoPro.Infrastructure.IoC;
using AgendamentoPro.Infrastructure.Middlewares;
using AgendamentoPro.Infrastructure.Services.WhatsApp;
using FluentValidation;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

// Template inclui propriedades injetadas pelo LogEnrichmentMiddleware (CorrelationId, TenantId, UserId)
// + Environment/MachineName (úteis em deploy multi-instância).
const string logTemplate =
    "[{Timestamp:HH:mm:ss} {Level:u3}] env={Environment} host={MachineName} cid={CorrelationId} tenant={TenantId}/{TenantSlug} user={UserId} {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate: logTemplate)
    .WriteTo.File("logs/log-.txt",
        outputTemplate: logTemplate,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 50 * 1024 * 1024,
        rollOnFileSizeLimit: true)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();
    builder.Configuration.AddEnvironmentVariables();

    builder.Services.AddControllers(opts =>
    {
        opts.Filters.Add<FluentValidationFilter>();
    });
    builder.Services.AddScoped<FluentValidationFilter>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddEndpointsApiExplorer();

    // Registra todos os validators do assembly de Application via reflection.
    builder.Services.AddValidatorsFromAssembly(
        typeof(AgendamentoPro.Application.Validators.Auth.LoginValidator).Assembly);

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "AgendamentoPro API",
            Version = "v1",
            Description = "API genérica multi-tenant de agendamento online."
        });
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization. Exemplo: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement {
            { new OpenApiSecurityScheme { Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
        });
    });

    var jwt = builder.Configuration.GetSection("JwtSettings");
    var envSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
    var configSecret = jwt["SecretKey"];
    var secret = envSecret ?? configSecret;

    // Em produção, exigir secret de pelo menos 64 chars vindo de variável de ambiente.
    // Em Development, aceita o de appsettings (mas avisa).
    if (!builder.Environment.IsDevelopment())
    {
        if (string.IsNullOrWhiteSpace(envSecret))
            throw new InvalidOperationException(
                "Em produção, JWT_SECRET_KEY deve ser definido como variável de ambiente (mínimo 64 chars).");
        if (envSecret.Length < 64)
            throw new InvalidOperationException(
                "JWT_SECRET_KEY deve ter no mínimo 64 caracteres em produção.");
    }
    if (string.IsNullOrWhiteSpace(secret))
        throw new InvalidOperationException("JWT SecretKey não configurado.");
    if (secret.Length < 32)
        throw new InvalidOperationException("JWT SecretKey deve ter no mínimo 32 caracteres.");

    builder.Services.AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30) // tolerância pra dessincronia de relógio
        };

        // Cross-tenant guard: rejeita token cujo claim tenantId NÃO bate com o tenant
        // resolvido pelo path (/api/t/{slug}/...) ou header X-Tenant-Slug.
        // SuperAdmin (sem tenantId) é exceção — pode acessar qualquer tenant.
        opt.Events = new JwtBearerEvents
        {
            // WebSocket não aceita cabeçalho Authorization do navegador — por isso o
            // cliente do SignalR manda o token em ?access_token=. Sem ler daqui, o
            // hub responde 401 e o realtime NUNCA conecta (o negotiate passa, o
            // upgrade não; o sino de notificações fica mudo sem erro no servidor).
            //
            // Restrito a /hubs de propósito: aceitar token por query string nas rotas
            // normais da API o faria vazar em log de acesso, histórico e Referer.
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token)
                    && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = token;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async ctx =>
            {
                var claims = ctx.Principal!;
                var tenantClaimStr = claims.FindFirst("tenantId")?.Value;
                var role = claims.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role == "SuperAdmin" || string.IsNullOrEmpty(tenantClaimStr))
                    return; // sem amarração de tenant
                if (!int.TryParse(tenantClaimStr, out var tokenTenantId))
                {
                    ctx.Fail("Token com tenantId inválido.");
                    return;
                }

                // Resolve tenant da request (path /api/t/{slug}/... ou header)
                int? requestTenantId = null;
                var tenants = ctx.HttpContext.RequestServices
                    .GetService(typeof(AgendamentoPro.Core.Interfaces.Database.Repositories.ITenantRepository))
                    as AgendamentoPro.Core.Interfaces.Database.Repositories.ITenantRepository;
                var path = ctx.HttpContext.Request.Path.Value ?? "";
                var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                string slugDoRequest = null;
                // Suporta /api/t/{slug}/... e /api/v{N}/t/{slug}/...
                if (parts.Length >= 3 && parts[0].Equals("api", StringComparison.OrdinalIgnoreCase))
                {
                    if (parts[1].Equals("t", StringComparison.OrdinalIgnoreCase))
                        slugDoRequest = parts[2];
                    else if (parts.Length >= 4 && parts[1].StartsWith("v", StringComparison.OrdinalIgnoreCase)
                        && parts[2].Equals("t", StringComparison.OrdinalIgnoreCase))
                        slugDoRequest = parts[3];
                }
                else if (ctx.HttpContext.Request.Headers.TryGetValue("X-Tenant-Slug", out var slugHeader))
                {
                    slugDoRequest = slugHeader.ToString();
                }

                if (!string.IsNullOrEmpty(slugDoRequest) && tenants != null)
                {
                    var t = await tenants.GetBySlugAsync(slugDoRequest);
                    if (t != null) requestTenantId = t.TenId;
                }

                if (requestTenantId.HasValue && requestTenantId.Value != tokenTenantId)
                {
                    ctx.Fail("Token não autoriza acesso a este tenant.");
                }
            }
        };
    });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("SuperAdmin", p => p.RequireRole("SuperAdmin"))
        .AddPolicy("AdminTenant", p => p.RequireRole("Administrador", "SuperAdmin"))
        .AddPolicy("Atendente", p => p.RequireRole("Atendente", "Administrador", "SuperAdmin"));

    // CORS: política do app frontend (whitelist explícita) e política aberta apenas
    // para endpoints públicos GET (catálogo público do tenant). Webhooks não usam CORS
    // pois são chamados server-to-server pelo gateway, não pelo browser.
    var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',', StringSplitOptions.RemoveEmptyEntries)
        ?? new[] { "http://localhost:4200", "http://localhost:5173" };

    // Rate limiting: protege endpoints de autenticação contra brute force
    // e webhooks contra flood. Particionado por tenant+IP — abuso em um tenant
    // não consome cota dos outros, e atacante alternando IP ainda fica preso
    // ao limite por tenant.
    static string ResolverPartitionKey(HttpContext httpCtx)
    {
        var ip = httpCtx.Connection.RemoteIpAddress?.ToString() ?? "anon";
        // Tenant resolvido pelo path /api/t/{slug}/... ou header X-Tenant-Slug
        var path = httpCtx.Request.Path.Value ?? "";
        string slug = "_";
        var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && parts[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            if (parts[1].Equals("t", StringComparison.OrdinalIgnoreCase))
                slug = parts[2];
            else if (parts.Length >= 4 && parts[1].StartsWith("v", StringComparison.OrdinalIgnoreCase)
                && parts[2].Equals("t", StringComparison.OrdinalIgnoreCase))
                slug = parts[3];
        }
        if (slug == "_" && httpCtx.Request.Headers.TryGetValue("X-Tenant-Slug", out var h))
            slug = h.ToString();
        return $"{slug}|{ip}";
    }

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (ctx, token) =>
        {
            ctx.HttpContext.Response.Headers["Retry-After"] = "60";
            await ctx.HttpContext.Response.WriteAsJsonAsync(
                new { message = "Muitas requisições. Tente novamente em alguns segundos." }, token);
        };

        // 5 tentativas por minuto por (tenant, IP) - login/refresh/forgot/reset
        options.AddPolicy("auth", httpCtx =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ResolverPartitionKey(httpCtx),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 5,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

        // OTP por WhatsApp: 3 envios + 5 validações por minuto por (tenant, IP).
        // Mais estrito que "auth" porque cada envio dispara WhatsApp (custo + spam).
        options.AddPolicy("otp", httpCtx =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ResolverPartitionKey(httpCtx),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 8,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

        // 60 webhooks por minuto por IP - tolera retries do gateway sem deixar abusar.
        // Webhooks não têm tenant no path, então usa só IP.
        options.AddPolicy("webhook", httpCtx =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpCtx.Connection.RemoteIpAddress?.ToString() ?? "anon",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 60,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

        // Default geral: 120 req/min por (tenant, IP)
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpCtx =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ResolverPartitionKey(httpCtx),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 120,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AppFrontend", p => p
            .WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            // X-Requested-With: o cliente do SignalR manda esse cabeçalho no
            // negotiate. Sem ele na lista, o preflight falha e o realtime NUNCA
            // conecta — o sino de notificações fica mudo e o erro só aparece no
            // console do navegador, nunca no log do servidor.
            .WithHeaders("Authorization", "Content-Type", "X-Tenant-Slug", "X-Correlation-Id", "Accept",
                         "X-Requested-With", "X-Signalr-User-Agent")
            .WithExposedHeaders("X-Correlation-Id")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
    });

    // Validação de URLs em produção: APP_PUBLIC_URL é callback dos gateways (HTTPS
    // obrigatório pelos próprios gateways). APP_FRONTEND_URL é incluído em links
    // de reset de senha, validação de OTP/avaliação enviados por email/WhatsApp —
    // se for HTTP, o token vai em texto claro pela rede do cliente.
    var appPublicUrl = Environment.GetEnvironmentVariable("APP_PUBLIC_URL")
        ?? builder.Configuration["App:PublicUrl"];
    var appFrontendUrl = Environment.GetEnvironmentVariable("APP_FRONTEND_URL")
        ?? builder.Configuration["App:FrontendUrl"];
    if (!builder.Environment.IsDevelopment())
    {
        if (string.IsNullOrWhiteSpace(appPublicUrl))
            throw new InvalidOperationException(
                "APP_PUBLIC_URL deve ser definido em produção (URL pública usada como callback dos gateways).");
        if (!appPublicUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "APP_PUBLIC_URL deve usar HTTPS em produção (callbacks de gateway não aceitam HTTP).");
        // APP_FRONTEND_URL é opcional (cai pra APP_PUBLIC_URL se vazio). Se definido,
        // exigir HTTPS — token de reset/OTP/avaliação seguiriam por canal cifrado.
        if (!string.IsNullOrWhiteSpace(appFrontendUrl)
            && !appFrontendUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "APP_FRONTEND_URL deve usar HTTPS em produção — links de reset/OTP/avaliação vão por email/WhatsApp.");
        }
    }

    builder.Services.WireUp(builder.Configuration);

    // SignalR para notificações realtime ao admin (aba aberta) + Web Push (aba fechada).
    // O `NotificacaoRealtimeComposto` dispara os dois em paralelo; Web Push é no-op
    // se VAPID_PUBLIC_KEY/VAPID_PRIVATE_KEY não estão configurados.
    builder.Services.AddSignalR();
    builder.Services.AddScoped<AgendamentoPro.API.Services.Realtime.SignalRNotificacaoRealtime>();
    builder.Services.AddScoped<AgendamentoPro.Core.Interfaces.Services.INotificacaoRealtime,
        AgendamentoPro.API.Services.Realtime.NotificacaoRealtimeComposto>();

    // OutputCache: cacheia respostas de endpoints públicos GET (catálogo, avaliações)
    // por 60s. Reduz hit no banco em landing pages com tráfego.
    builder.Services.AddOutputCache(options =>
    {
        options.AddPolicy("PublicoCurto", b => b.Expire(TimeSpan.FromSeconds(60))
            .SetVaryByRouteValue("slug").Tag("publico"));
        options.AddPolicy("PublicoLongo", b => b.Expire(TimeSpan.FromMinutes(5))
            .SetVaryByRouteValue("slug").Tag("publico"));
    });

    // Hangfire: storage persistente (jobs sobrevivem a restart).
    //  - Provider Sqlite  → arquivo separado "hangfire.db" (não compartilha com EF Migrations)
    //  - Provider SqlServer → mesma connection string (Hangfire cria schema [HangFire] sozinho)
    //  - Override:  HANGFIRE_STORAGE=Memory  (somente dev/testes; jobs somem em restart)
    var hangfireMode = (Environment.GetEnvironmentVariable("HANGFIRE_STORAGE")
        ?? builder.Configuration["Hangfire:Storage"] ?? "").Trim();
    var hangfireDbProvider = (builder.Configuration["Database:Provider"] ?? "Sqlite").Trim();
    builder.Services.AddHangfire(cfg =>
    {
        cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
           .UseSimpleAssemblyNameTypeSerializer()
           .UseRecommendedSerializerSettings();

        if (hangfireMode.Equals("Memory", StringComparison.OrdinalIgnoreCase))
        {
            cfg.UseMemoryStorage();
        }
        else if (hangfireDbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            var connStr = builder.Configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:Default obrigatório para Hangfire em SqlServer.");
            cfg.UseSqlServerStorage(connStr, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                PrepareSchemaIfNecessary = true
            });
        }
        else
        {
            // Default: arquivo `hangfire.db` no mesmo diretório do banco principal.
            // Garante que, em Docker, ele caia no volume persistente (/data) — não em /app.
            var hangfirePath = Environment.GetEnvironmentVariable("HANGFIRE_DB_PATH")
                ?? builder.Configuration["Hangfire:DbPath"]
                ?? DeriveHangfireDbPath(builder.Configuration.GetConnectionString("Default"));
            cfg.UseSQLiteStorage(hangfirePath);
        }
    });
    builder.Services.AddHangfireServer(opts =>
    {
        opts.WorkerCount = 2;
        opts.ServerName = $"agendamentopro-{Environment.MachineName}";
    });

    // Health checks: liveness (processo respondendo) + readiness (banco respondendo)
    // + integrações externas (informativo, sempre healthy quando "no-op").
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AgendamentoProDbContext>(
            name: "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "ready" })
        .AddCheck<AgendamentoPro.API.Health.IntegracoesHealthCheck>(
            name: "integracoes",
            tags: new[] { "ready", "integracoes" });

    // Atrás de proxy reverso (nginx/Traefik), respeitar X-Forwarded-* para
    // que ASP.NET Core veja scheme/IP corretos. Limpa redes/proxies conhecidos
    // para aceitar do compose/docker/orquestrador qualquer (necessário com Docker).
    builder.Services.Configure<ForwardedHeadersOptions>(opts =>
    {
        opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        opts.KnownNetworks.Clear();
        opts.KnownProxies.Clear();
    });

    var app = builder.Build();

    // ForwardedHeaders DEVE vir antes de qualquer middleware que use scheme/IP
    app.UseForwardedHeaders();

    app.InitializeDatabase();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgendamentoPro API v1");
    });

    app.UseRouting();

    // Servir arquivos enviados pelo upload de fotos. Caminho default = AppContext.BaseDirectory/uploads
    // (configurável via UPLOADS_PATH). URLs ficam expostas em /uploads/{tenantId}/{agendamentoId}/{nome}.
    var uploadsPath = Environment.GetEnvironmentVariable("UPLOADS_PATH")
        ?? builder.Configuration["Uploads:Path"]
        ?? Path.Combine(AppContext.BaseDirectory, "uploads");
    Directory.CreateDirectory(uploadsPath);
    app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });

    app.UseCors("AppFrontend");
    app.UseRateLimiter();
    app.UseOutputCache();

    app.UseCorrelationId();
    app.UseErrorHandlingMiddleware();
    app.UseAuthentication();
    app.UseTenantResolution();
    app.UseLogEnrichment();
    app.UseAssinaturaGuard();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<AgendamentoPro.API.Hubs.NotificacoesHub>("/hubs/notificacoes");

    // Hangfire dashboard - /hangfire (autenticado, role SuperAdmin/Administrador)
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuth() },
        DashboardTitle = "AgendamentoPro Jobs",
        DisplayStorageConnectionString = false
    });

    // Recurring jobs
    RecurringJob.AddOrUpdate<LembreteJob>(
        "lembretes-24h-2h",
        job => job.ExecutarAsync(CancellationToken.None),
        "*/5 * * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    // Purge de logs de auditoria > 12 meses, todo dia às 4h UTC (LGPD/minimização)
    RecurringJob.AddOrUpdate<AgendamentoPro.Infrastructure.Services.Manutencao.AuditPurgeJob>(
        "purge-audit-log",
        job => job.ExecutarAsync(CancellationToken.None),
        "0 4 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    // Backup do SQLite + uploads, todo dia às 3h UTC
    RecurringJob.AddOrUpdate<AgendamentoPro.Infrastructure.Services.Manutencao.BackupJob>(
        "backup-diario",
        job => job.ExecutarAsync(CancellationToken.None),
        "0 3 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    // Grace period de assinaturas SaaS: marca Atrasada → ReadOnly → Expirada conforme
    // janelas de 8 e 22 dias. Roda às 5h UTC pra não competir com backup (3h) e purge (4h).
    RecurringJob.AddOrUpdate<AgendamentoPro.Infrastructure.Services.Assinaturas.AssinaturaStatusJob>(
        "assinatura-status",
        job => job.ExecutarAsync(CancellationToken.None),
        "0 5 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    // /api/health/live - liveness (sempre OK se o processo respondeu)
    app.MapHealthChecks("/api/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = WriteHealthResponse
    }).AllowAnonymous();

    // /api/health/ready - readiness (banco e dependências)
    app.MapHealthChecks("/api/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthResponse
    }).AllowAnonymous();

    app.Run();

    // Coloca hangfire.db ao lado do agendamento.db. Em Docker, o connection string aponta
    // pra /data/agendamento.db (volume persistente) — Hangfire então vai pra /data/hangfire.db
    // automaticamente sem precisar de configuração extra.
    static string DeriveHangfireDbPath(string mainConnectionString)
    {
        const string defaultPath = "hangfire.db";
        if (string.IsNullOrWhiteSpace(mainConnectionString)) return Path.Combine(AppContext.BaseDirectory, defaultPath);
        // Extrai "Data Source=..." da connection string SQLite
        var parts = mainConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            if (p.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("DataSource=", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase))
            {
                var valor = p.Substring(p.IndexOf('=') + 1).Trim();
                var dir = Path.GetDirectoryName(valor);
                if (!string.IsNullOrWhiteSpace(dir)) return Path.Combine(dir, defaultPath);
                return defaultPath; // mesmo diretório de trabalho do main DB
            }
        }
        return Path.Combine(AppContext.BaseDirectory, defaultPath);
    }

    static Task WriteHealthResponse(HttpContext ctx, HealthReport report)
    {
        ctx.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                error = e.Value.Exception?.Message
            })
        };
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação terminou inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}

using AgendamentoPro.API.Filters;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using AgendamentoPro.Infrastructure.IoC;
using AgendamentoPro.Infrastructure.Middlewares;
using AgendamentoPro.Infrastructure.Services.WhatsApp;
using FluentValidation;
using Hangfire;
using Hangfire.MemoryStorage;
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
                if (parts.Length >= 3 && parts[0].Equals("api", StringComparison.OrdinalIgnoreCase)
                    && parts[1].Equals("t", StringComparison.OrdinalIgnoreCase))
                {
                    slugDoRequest = parts[2];
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
        if (parts.Length >= 3 && parts[0].Equals("api", StringComparison.OrdinalIgnoreCase)
            && parts[1].Equals("t", StringComparison.OrdinalIgnoreCase))
            slug = parts[2];
        else if (httpCtx.Request.Headers.TryGetValue("X-Tenant-Slug", out var h))
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
            .WithHeaders("Authorization", "Content-Type", "X-Tenant-Slug", "X-Correlation-Id", "Accept")
            .WithExposedHeaders("X-Correlation-Id")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
    });

    // Validação de APP_PUBLIC_URL em produção: deve ser HTTPS para callbacks dos gateways.
    var appPublicUrl = Environment.GetEnvironmentVariable("APP_PUBLIC_URL")
        ?? builder.Configuration["App:PublicUrl"];
    if (!builder.Environment.IsDevelopment())
    {
        if (string.IsNullOrWhiteSpace(appPublicUrl))
            throw new InvalidOperationException(
                "APP_PUBLIC_URL deve ser definido em produção (URL pública usada como callback dos gateways).");
        if (!appPublicUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "APP_PUBLIC_URL deve usar HTTPS em produção (callbacks de gateway não aceitam HTTP).");
    }

    builder.Services.WireUp(builder.Configuration);

    // Hangfire: storage in-memory (sem dependência externa). Para produção real
    // com persistência de jobs, troque por UseSqlServerStorage / UsePostgreSqlStorage.
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseMemoryStorage());
    builder.Services.AddHangfireServer(opts =>
    {
        opts.WorkerCount = 2;
        opts.ServerName = $"agendamentopro-{Environment.MachineName}";
    });

    // Health checks: liveness (processo respondendo) + readiness (banco respondendo).
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AgendamentoProDbContext>(
            name: "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "ready" });

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

    app.UseErrorHandlingMiddleware();
    app.UseAuthentication();
    app.UseTenantResolution();
    app.UseLogEnrichment();
    app.UseAuthorization();
    app.MapControllers();

    // Hangfire dashboard - /hangfire (autenticado, role SuperAdmin/Administrador)
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuth() },
        DashboardTitle = "AgendamentoPro Jobs",
        DisplayStorageConnectionString = false
    });

    // Recurring job: lembretes a cada 5 min (substitui LembreteBackgroundService)
    RecurringJob.AddOrUpdate<LembreteJob>(
        "lembretes-24h-2h",
        job => job.ExecutarAsync(CancellationToken.None),
        "*/5 * * * *",
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

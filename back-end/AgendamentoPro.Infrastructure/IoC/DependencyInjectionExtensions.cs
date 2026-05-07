using AgendamentoPro.Application.Interfaces.Agendamentos;
using AgendamentoPro.Application.Interfaces.Auth;
using AgendamentoPro.Application.Interfaces.Clientes;
using AgendamentoPro.Application.Interfaces.Dashboard;
using AgendamentoPro.Application.Interfaces.Pagamentos;
using AgendamentoPro.Application.Interfaces.Recursos;
using AgendamentoPro.Application.Interfaces.Relatorios;
using AgendamentoPro.Application.Interfaces.Servicos;
using AgendamentoPro.Application.Interfaces.Tenants;
using AgendamentoPro.Application.UseCases.Agendamentos;
using AgendamentoPro.Application.UseCases.Auth;
using AgendamentoPro.Application.UseCases.Clientes;
using AgendamentoPro.Application.UseCases.Dashboard;
using AgendamentoPro.Application.UseCases.Pagamentos;
using AgendamentoPro.Application.UseCases.Recursos;
using AgendamentoPro.Application.UseCases.Relatorios;
using AgendamentoPro.Application.UseCases.Servicos;
using AgendamentoPro.Application.UseCases.Tenants;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using AgendamentoPro.Infrastructure.Database.EntityFramework;
using AgendamentoPro.Infrastructure.Database.EntityFramework.Repositories;
using AgendamentoPro.Infrastructure.Database.UnitOfWork;
using AgendamentoPro.Infrastructure.Services.Auth;
using AgendamentoPro.Infrastructure.Services.Cache;
using AgendamentoPro.Infrastructure.Services.Email;
using AgendamentoPro.Infrastructure.Services.Pagamento;
using AgendamentoPro.Infrastructure.Services.Storage;
using AgendamentoPro.Infrastructure.Services.Tenant;
using AgendamentoPro.Infrastructure.Services.WhatsApp;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Infrastructure.IoC
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection WireUp(this IServiceCollection services, IConfiguration config)
        {
            // DbContext + AuditInterceptor (registra LogAuditoria por SaveChanges)
            services.AddSingleton<AuditInterceptor>();
            var provider = config["Database:Provider"] ?? "Sqlite";
            var conn = config.GetConnectionString("Default") ?? "Data Source=agendamento.db";
            services.AddDbContext<AgendamentoProDbContext>((sp, opt) =>
            {
                if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                    opt.UseSqlServer(conn);
                else
                    opt.UseSqlite(conn);
                opt.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            });

            // UoW
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Tenant context (scoped — populado pelo middleware)
            services.AddScoped<ITenantContext, TenantContext>();

            // Repositórios
            services.AddScoped<ITenantRepository, TenantRepository>();
            services.AddScoped<IConfiguracaoTenantRepository, ConfiguracaoTenantRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
            services.AddScoped<IServicoRepository, ServicoRepository>();
            services.AddScoped<IRecursoRepository, RecursoRepository>();
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
            services.AddScoped<IPagamentoRepository, PagamentoRepository>();
            services.AddScoped<IWebhookEventoRepository, WebhookEventoRepository>();
            services.AddScoped<IFotoAgendamentoRepository, FotoAgendamentoRepository>();
            services.AddScoped<IAvaliacaoRepository, AvaliacaoRepository>();
            services.AddScoped<IComboRepository, ComboRepository>();
            services.AddScoped<IHorarioFuncionamentoRepository, HorarioFuncionamentoRepository>();

            // Serviços de domínio
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IDisponibilidadeService, DisponibilidadeService>();
            services.AddScoped<ITenantSeeder, DemoDataSeeder>();
            services.AddSingleton<IFotoStorage, LocalFotoStorage>();
            services.AddSingleton<IEmailSender, SmtpEmailSender>();

            // Cache em memória com isolamento por tenant (evita bleed-through)
            services.AddMemoryCache();
            services.AddScoped<ITenantCache, TenantAwareMemoryCache>();

            // Integrações externas com HttpClient nomeado (resiliência via Polly opcional)
            services.AddHttpClient<IGatewayPagamento, MercadoPagoGateway>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddHttpClient<INotificadorWhatsApp, WhatsAppCloudNotificador>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(15);
            });

            // Background service que envia lembretes 24h e 2h antes do agendamento.
            services.AddHostedService<LembreteBackgroundService>();

            // UseCases
            services.AddScoped<ILoginUseCase, LoginUseCase>();
            services.AddScoped<ISolicitarResetSenhaUseCase, SolicitarResetSenhaUseCase>();
            services.AddScoped<IRedefinirSenhaUseCase, RedefinirSenhaUseCase>();
            services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
            services.AddScoped<ICriarTenantUseCase, CriarTenantUseCase>();
            services.AddScoped<IConsultarTenantUseCase, ConsultarTenantUseCase>();
            services.AddScoped<IAtualizarTenantUseCase, AtualizarTenantUseCase>();
            services.AddScoped<ICadastrarServicoUseCase, CadastrarServicoUseCase>();
            services.AddScoped<IAtualizarServicoUseCase, AtualizarServicoUseCase>();
            services.AddScoped<IConsultarServicoUseCase, ConsultarServicoUseCase>();
            services.AddScoped<IInativarServicoUseCase, InativarServicoUseCase>();
            services.AddScoped<ICadastrarRecursoUseCase, CadastrarRecursoUseCase>();
            services.AddScoped<IAtualizarRecursoUseCase, AtualizarRecursoUseCase>();
            services.AddScoped<IConsultarRecursoUseCase, ConsultarRecursoUseCase>();
            services.AddScoped<IInativarRecursoUseCase, InativarRecursoUseCase>();
            services.AddScoped<ICadastrarClienteUseCase, CadastrarClienteUseCase>();
            services.AddScoped<IAtualizarClienteUseCase, AtualizarClienteUseCase>();
            services.AddScoped<IConsultarClienteUseCase, ConsultarClienteUseCase>();
            services.AddScoped<ICriarAgendamentoUseCase, CriarAgendamentoUseCase>();
            services.AddScoped<IConsultarSlotsUseCase, ConsultarSlotsUseCase>();
            services.AddScoped<IConsultarAgendamentoUseCase, ConsultarAgendamentoUseCase>();
            services.AddScoped<IReagendarUseCase, ReagendarUseCase>();
            services.AddScoped<ICancelarAgendamentoUseCase, CancelarAgendamentoUseCase>();
            services.AddScoped<IAlterarStatusAgendamentoUseCase, AlterarStatusAgendamentoUseCase>();
            services.AddScoped<IProcessarWebhookPagamentoUseCase, ProcessarWebhookPagamentoUseCase>();
            services.AddScoped<IFotoAgendamentoUseCase, FotoAgendamentoUseCase>();
            services.AddScoped<IAvaliacaoUseCase, AvaliacaoUseCase>();
            services.AddScoped<IComboUseCase, ComboUseCase>();
            services.AddScoped<IAgendarComboUseCase, AgendarComboUseCase>();
            services.AddScoped<IDashboardUseCase, DashboardUseCase>();
            services.AddScoped<IRelatoriosUseCase, RelatoriosUseCase>();

            return services;
        }

        public static void InitializeDatabase(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AgendamentoProDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AgendamentoProDbContext>>();

            // Migrações: aplica todas as pendentes em produção. Schema sempre evolui via
            // `dotnet ef migrations add <Nome>` -> commit -> deploy.
            // Se o assembly NÃO tiver migrations (cenário improvável - só durante setup inicial
            // antes da primeira migration ser comitada), usa EnsureCreated como fallback.
            if (ctx.Database.GetMigrations().Any())
            {
                var pendentes = ctx.Database.GetPendingMigrations().ToList();
                if (pendentes.Count > 0)
                {
                    logger.LogInformation("Aplicando {Count} migration(s) pendente(s): {Migrations}",
                        pendentes.Count, string.Join(", ", pendentes));
                    ctx.Database.Migrate();
                }
            }
            else
            {
                logger.LogWarning("Nenhuma migration encontrada no assembly. Usando EnsureCreated como fallback "
                    + "(gere a migration inicial com `dotnet ef migrations add InitialCreate`).");
                ctx.Database.EnsureCreated();
            }

            // Seed do SuperAdmin (apenas se não existir nenhum)
            if (!ctx.Usuarios.Any(u => u.UsuPerfil == Core.Enums.PerfilUsuario.SuperAdmin))
            {
                var email = PrimeiroNaoVazio(
                    Environment.GetEnvironmentVariable("SUPERADMIN_EMAIL"),
                    config["SuperAdmin:Email"],
                    "admin@agendamentopro.local");

                var senha = PrimeiroNaoVazio(
                    Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD"),
                    config["SuperAdmin:Password"]);

                bool senhaGerada = false;
                if (string.IsNullOrWhiteSpace(senha))
                {
                    // Gera senha aleatória forte e loga UMA VEZ no console (operador deve trocar)
                    senha = GerarSenhaAleatoria(20);
                    senhaGerada = true;
                }

                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var super = new Core.Entities.Usuarios.Usuario(null,
                    "Super Admin", email, hasher.Hash(senha),
                    Core.Enums.PerfilUsuario.SuperAdmin, null);
                ctx.Usuarios.Add(super);
                ctx.SaveChanges();

                if (senhaGerada)
                {
                    logger.LogWarning("==============================================================");
                    logger.LogWarning("SUPER ADMIN criado com senha aleatória. ANOTE AGORA:");
                    logger.LogWarning("  E-mail: {Email}", email);
                    logger.LogWarning("  Senha:  {Senha}", senha);
                    logger.LogWarning("Defina SUPERADMIN_PASSWORD para usar senha fixa em produção.");
                    logger.LogWarning("==============================================================");
                }
                else
                {
                    logger.LogInformation("SuperAdmin criado com e-mail '{Email}' e senha definida via configuração.", email);
                }
            }
        }

        private static string PrimeiroNaoVazio(params string[] valores)
        {
            foreach (var v in valores)
                if (!string.IsNullOrWhiteSpace(v)) return v;
            return null;
        }

        private static string GerarSenhaAleatoria(int tamanho)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%&*";
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[tamanho];
            rng.GetBytes(bytes);
            var sb = new System.Text.StringBuilder(tamanho);
            foreach (var b in bytes) sb.Append(chars[b % chars.Length]);
            return sb.ToString();
        }
    }
}

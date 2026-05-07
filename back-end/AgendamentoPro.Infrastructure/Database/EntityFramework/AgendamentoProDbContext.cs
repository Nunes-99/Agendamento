using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Common;
using AgendamentoPro.Core.Entities.Horarios;
using AgendamentoPro.Core.Entities.Notificacoes;
using AgendamentoPro.Core.Entities.Pagamentos;
using AgendamentoPro.Core.Entities.Recursos;
using AgendamentoPro.Core.Entities.RefreshTokens;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AgendamentoPro.Infrastructure.Database.EntityFramework
{
    public class AgendamentoProDbContext : DbContext
    {
        public AgendamentoProDbContext(DbContextOptions<AgendamentoProDbContext> options) : base(options) { }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<ConfiguracaoTenant> ConfiguracoesTenant { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }
        public DbSet<Servico> Servicos { get; set; }
        public DbSet<Recurso> Recursos { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Agendamento> Agendamentos { get; set; }
        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<WebhookEvento> WebhookEventos { get; set; }
        public DbSet<FotoAgendamento> FotosAgendamento { get; set; }
        public DbSet<Avaliacao> Avaliacoes { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<ComboServico> ComboServicos { get; set; }
        public DbSet<ListaEspera> ListaEspera { get; set; }
        public DbSet<Cupom> Cupons { get; set; }
        public DbSet<HorarioFuncionamento> HorariosFuncionamento { get; set; }
        public DbSet<BloqueioAgenda> BloqueiosAgenda { get; set; }
        public DbSet<Notificacao> Notificacoes { get; set; }
        public DbSet<LogAuditoria> LogsAuditoria { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // Soft Delete global: para toda entidade que herda de SoftDeletableEntity,
            // adiciona automaticamente um QueryFilter que esconde linhas com Excluido=true.
            // Para "ressuscitar" uma query: use .IgnoreQueryFilters().
            foreach (var entityType in mb.Model.GetEntityTypes())
            {
                if (typeof(AgendamentoPro.Core.Interfaces.Common.ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var prop = System.Linq.Expressions.Expression.Property(parameter, nameof(AgendamentoPro.Core.Interfaces.Common.ISoftDeletable.Excluido));
                    var filter = System.Linq.Expressions.Expression.Lambda(
                        System.Linq.Expressions.Expression.Not(prop), parameter);
                    mb.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }

            mb.Entity<Tenant>(e =>
            {
                e.ToTable("Tenant");
                e.HasKey(x => x.TenId);
                e.Property(x => x.TenId).ValueGeneratedOnAdd();
                e.Property(x => x.TenNome).HasMaxLength(200).IsRequired();
                e.Property(x => x.TenSlug).HasMaxLength(80).IsRequired();
                e.HasIndex(x => x.TenSlug).IsUnique();
                e.Property(x => x.TenSegmento).HasMaxLength(100);
                e.Property(x => x.TenCnpj).HasMaxLength(18);
                e.Property(x => x.TenEmail).HasMaxLength(255).IsRequired();
                e.Property(x => x.TenTelefone).HasMaxLength(30);
                e.Property(x => x.TenWhatsApp).HasMaxLength(30);
                e.Property(x => x.TenEndereco).HasMaxLength(255);
                e.Property(x => x.TenCidade).HasMaxLength(100);
                e.Property(x => x.TenEstado).HasMaxLength(2);
                e.Property(x => x.TenCep).HasMaxLength(10);
                e.Property(x => x.TenLogoUrl).HasMaxLength(500);
                e.Property(x => x.TenBannerUrl).HasMaxLength(500);
                e.Property(x => x.TenFaviconUrl).HasMaxLength(500);
                e.Property(x => x.TenCorPrimaria).HasMaxLength(20);
                e.Property(x => x.TenCorSecundaria).HasMaxLength(20);
                e.Property(x => x.TenCorAcento).HasMaxLength(20);
                e.Property(x => x.TenFonte).HasMaxLength(50);
                e.Property(x => x.TenDescricao).HasMaxLength(2000);
                e.Property(x => x.TenPercentualEntrada).HasPrecision(5, 2);
            });

            mb.Entity<ConfiguracaoTenant>(e =>
            {
                e.ToTable("ConfiguracaoTenant");
                e.HasKey(x => x.CfgId);
                e.Property(x => x.CfgChave).HasMaxLength(100).IsRequired();
                e.Property(x => x.CfgValor).HasMaxLength(4000);
                e.Property(x => x.CfgGrupo).HasMaxLength(50);
                e.HasIndex(x => new { x.R_TenId, x.CfgChave }).IsUnique();
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
            });

            mb.Entity<Usuario>(e =>
            {
                e.ToTable("Usuario");
                e.HasKey(x => x.UsuId);
                e.Property(x => x.UsuId).ValueGeneratedOnAdd();
                e.Property(x => x.UsuNome).HasMaxLength(200).IsRequired();
                e.Property(x => x.UsuEmail).HasMaxLength(255).IsRequired();
                e.Property(x => x.UsuSenha).HasMaxLength(255).IsRequired();
                e.Property(x => x.UsuPerfil).HasMaxLength(50).IsRequired();
                e.Property(x => x.UsuTelefone).HasMaxLength(30);
                e.HasIndex(x => x.UsuEmail).IsUnique();
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
            });

            mb.Entity<RefreshToken>(e =>
            {
                e.ToTable("RefreshToken");
                e.HasKey(x => x.RefId);
                e.Property(x => x.RefToken).HasMaxLength(200).IsRequired();
                e.Property(x => x.RefJwtId).HasMaxLength(100);
                e.HasIndex(x => x.RefToken).IsUnique();
                e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.R_UsuId);
            });

            mb.Entity<PasswordReset>(e =>
            {
                e.ToTable("PasswordReset");
                e.HasKey(x => x.RpsId);
                e.Property(x => x.RpsToken).HasMaxLength(200).IsRequired();
                e.HasIndex(x => x.RpsToken).IsUnique();
                e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.R_UsuId);
            });

            mb.Entity<Servico>(e =>
            {
                e.ToTable("Servico");
                e.HasKey(x => x.SerId);
                e.Property(x => x.SerNome).HasMaxLength(150).IsRequired();
                e.Property(x => x.SerDescricao).HasMaxLength(1000);
                e.Property(x => x.SerCategoria).HasMaxLength(100);
                e.Property(x => x.SerImagemUrl).HasMaxLength(500);
                e.Property(x => x.SerPreco).HasPrecision(10, 2);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
                e.HasIndex(x => new { x.R_TenId, x.SerAtivo });
            });

            mb.Entity<Recurso>(e =>
            {
                e.ToTable("Recurso");
                e.HasKey(x => x.RecId);
                e.Property(x => x.RecNome).HasMaxLength(150).IsRequired();
                e.Property(x => x.RecDescricao).HasMaxLength(500);
                e.Property(x => x.RecTipo).HasMaxLength(50);
                e.Property(x => x.RecImagemUrl).HasMaxLength(500);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
                e.HasIndex(x => new { x.R_TenId, x.RecAtivo });
            });

            mb.Entity<Cliente>(e =>
            {
                e.ToTable("Cliente");
                e.HasKey(x => x.CliId);
                e.Property(x => x.CliNome).HasMaxLength(200).IsRequired();
                e.Property(x => x.CliEmail).HasMaxLength(255);
                e.Property(x => x.CliTelefone).HasMaxLength(30);
                e.Property(x => x.CliWhatsApp).HasMaxLength(30);
                e.Property(x => x.CliCpf).HasMaxLength(14);
                e.Property(x => x.CliObservacao).HasMaxLength(1000);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
                e.HasIndex(x => new { x.R_TenId, x.CliTelefone });
                e.HasIndex(x => new { x.R_TenId, x.CliEmail });
            });

            mb.Entity<Agendamento>(e =>
            {
                e.ToTable("Agendamento");
                e.HasKey(x => x.AgeId);
                e.Property(x => x.AgeId).ValueGeneratedOnAdd();
                e.Property(x => x.AgeValorTotal).HasPrecision(10, 2);
                e.Property(x => x.AgeValorEntrada).HasPrecision(10, 2);
                e.Property(x => x.AgePercentualEntrada).HasPrecision(5, 2);
                e.Property(x => x.AgeStatus).HasConversion<int>();
                e.Property(x => x.AgePagamentoStatus).HasConversion<int>();
                e.Property(x => x.AgeObservacao).HasMaxLength(1000);
                e.Property(x => x.AgeMotivoCancelamento).HasMaxLength(500);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
                e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.R_CliId);
                e.HasOne(x => x.Servico).WithMany().HasForeignKey(x => x.R_SerId);
                e.HasOne(x => x.Recurso).WithMany().HasForeignKey(x => x.R_RecId);
                // Índice único por recurso/data/hora — proteção contra concorrência
                e.HasIndex(x => new { x.R_RecId, x.AgeData, x.AgeHoraInicio }).IsUnique()
                    .HasFilter(null);
                e.HasIndex(x => new { x.R_TenId, x.AgeData });
                e.HasIndex(x => x.AgeGrupoComboId);
                e.HasIndex(x => x.AgeAcessoToken).IsUnique();
            });

            mb.Entity<Pagamento>(e =>
            {
                e.ToTable("Pagamento");
                e.HasKey(x => x.PagId);
                e.Property(x => x.PagValor).HasPrecision(10, 2);
                e.Property(x => x.PagForma).HasConversion<int>();
                e.Property(x => x.PagStatus).HasConversion<int>();
                e.Property(x => x.PagGateway).HasMaxLength(50);
                e.Property(x => x.PagGatewayId).HasMaxLength(200);
                e.Property(x => x.PagQrCode).HasMaxLength(4000);
                e.Property(x => x.PagLinkPagamento).HasMaxLength(500);
                e.Property(x => x.PagPayloadGateway);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
                e.HasOne(x => x.Agendamento).WithMany(a => a.Pagamentos).HasForeignKey(x => x.R_AgeId);
                // GatewayId único impede duplicação caso webhook chegue antes do escape do create
                e.HasIndex(x => x.PagGatewayId).IsUnique().HasFilter(null);
            });

            mb.Entity<WebhookEvento>(e =>
            {
                e.ToTable("WebhookEvento");
                e.HasKey(x => x.WhEvId);
                e.Property(x => x.WhEvGateway).HasMaxLength(50).IsRequired();
                e.Property(x => x.WhEvEventoId).HasMaxLength(200).IsRequired();
                e.Property(x => x.WhEvTipo).HasMaxLength(100);
                e.Property(x => x.WhEvPayload);
                // Idempotência: dois webhooks com mesmo (gateway, evento) não podem coexistir.
                e.HasIndex(x => new { x.WhEvGateway, x.WhEvEventoId }).IsUnique();
            });

            mb.Entity<FotoAgendamento>(e =>
            {
                e.ToTable("FotoAgendamento");
                e.HasKey(x => x.FotId);
                e.Property(x => x.FotTipo).HasConversion<int>();
                e.Property(x => x.FotUrl).HasMaxLength(500).IsRequired();
                e.Property(x => x.FotNomeOriginal).HasMaxLength(255);
                e.Property(x => x.FotContentType).HasMaxLength(100);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
                e.HasOne(x => x.Agendamento).WithMany().HasForeignKey(x => x.R_AgeId);
                e.HasIndex(x => new { x.R_TenId, x.R_AgeId });
            });

            mb.Entity<Combo>(e =>
            {
                e.ToTable("Combo");
                e.HasKey(x => x.ComId);
                e.Property(x => x.ComNome).HasMaxLength(150).IsRequired();
                e.Property(x => x.ComDescricao).HasMaxLength(1000);
                e.Property(x => x.ComImagemUrl).HasMaxLength(500);
                e.Property(x => x.ComPrecoPromocional).HasPrecision(10, 2);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
                e.HasIndex(x => new { x.R_TenId, x.ComAtivo });
            });
            mb.Entity<ComboServico>(e =>
            {
                e.ToTable("ComboServico");
                e.HasKey(x => x.ComServId);
                e.HasOne(x => x.Combo).WithMany(c => c.Servicos).HasForeignKey(x => x.R_ComId);
                e.HasOne(x => x.Servico).WithMany().HasForeignKey(x => x.R_SerId);
                e.HasIndex(x => new { x.R_ComId, x.R_SerId }).IsUnique();
            });

            mb.Entity<Cupom>(e =>
            {
                e.ToTable("Cupom");
                e.HasKey(x => x.CupId);
                e.Property(x => x.CupCodigo).HasMaxLength(50).IsRequired();
                e.Property(x => x.CupTipo).HasConversion<int>();
                e.Property(x => x.CupValor).HasPrecision(10, 2);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
                e.HasIndex(x => new { x.R_TenId, x.CupCodigo }).IsUnique();
            });

            mb.Entity<ListaEspera>(e =>
            {
                e.ToTable("ListaEspera");
                e.HasKey(x => x.LesId);
                e.Property(x => x.LesClienteNome).HasMaxLength(200).IsRequired();
                e.Property(x => x.LesClienteTelefone).HasMaxLength(30);
                e.Property(x => x.LesClienteEmail).HasMaxLength(255);
                e.Property(x => x.LesObservacao).HasMaxLength(500);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
                e.HasOne(x => x.Servico).WithMany().HasForeignKey(x => x.R_SerId);
                e.HasIndex(x => new { x.R_TenId, x.LesDataDesejada, x.LesNotificado });
            });

            mb.Entity<Avaliacao>(e =>
            {
                e.ToTable("Avaliacao");
                e.HasKey(x => x.AvaId);
                e.Property(x => x.AvaToken).IsRequired();
                e.Property(x => x.AvaComentario).HasMaxLength(1000);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
                e.HasOne(x => x.Agendamento).WithMany().HasForeignKey(x => x.R_AgeId);
                e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.R_CliId);
                e.HasIndex(x => x.AvaToken).IsUnique();
                e.HasIndex(x => x.R_AgeId).IsUnique(); // 1 avaliação por agendamento
                e.HasIndex(x => new { x.R_TenId, x.AvaRespondidoEm });
            });

            mb.Entity<HorarioFuncionamento>(e =>
            {
                e.ToTable("HorarioFuncionamento");
                e.HasKey(x => x.HorId);
                e.Property(x => x.HorDiaSemana).HasConversion<int>();
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
                e.HasIndex(x => new { x.R_TenId, x.HorDiaSemana }).IsUnique();
            });

            mb.Entity<BloqueioAgenda>(e =>
            {
                e.ToTable("BloqueioAgenda");
                e.HasKey(x => x.BloId);
                e.Property(x => x.BloMotivo).HasMaxLength(500);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
            });

            mb.Entity<Notificacao>(e =>
            {
                e.ToTable("Notificacao");
                e.HasKey(x => x.NotId);
                e.Property(x => x.NotCanal).HasMaxLength(20);
                e.Property(x => x.NotTipo).HasMaxLength(50);
                e.Property(x => x.NotDestinatario).HasMaxLength(200);
                e.Property(x => x.NotMensagem).HasMaxLength(2000);
                e.Property(x => x.NotStatus).HasMaxLength(20);
                e.Property(x => x.NotErro).HasMaxLength(1000);
                e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.R_TenId);
            });

            mb.Entity<LogAuditoria>(e =>
            {
                e.ToTable("LogAuditoria");
                e.HasKey(x => x.LogId);
                e.Property(x => x.LogUsuarioEmail).HasMaxLength(255);
                e.Property(x => x.LogIp).HasMaxLength(64);
                e.Property(x => x.LogCorrelationId).HasMaxLength(64);
                e.Property(x => x.LogTabela).HasMaxLength(100).IsRequired();
                e.Property(x => x.LogChave).HasMaxLength(100);
                e.Property(x => x.LogAcao).HasMaxLength(20).IsRequired();
                // Payloads JSON podem ser grandes — sem MaxLength explícito
                e.HasIndex(x => new { x.R_TenId, x.LogQuandoUtc });
                e.HasIndex(x => new { x.LogTabela, x.LogChave });
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (EhViolacaoUnicidade(ex))
            {
                throw new ConcorrenciaException("Conflito de unicidade no banco.");
            }
        }

        private static bool EhViolacaoUnicidade(DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("constraint failed", StringComparison.OrdinalIgnoreCase);
        }
    }
}

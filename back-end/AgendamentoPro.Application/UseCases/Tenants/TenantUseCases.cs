using AgendamentoPro.Application.InputModels.Tenants;
using AgendamentoPro.Application.Interfaces.Tenants;
using AgendamentoPro.Application.ViewModels.Tenants;
using AgendamentoPro.Core.Entities.Horarios;
using AgendamentoPro.Core.Entities.Recursos;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Application.UseCases.Tenants
{
    public class CriarTenantUseCase : ICriarTenantUseCase
    {
        private readonly ITenantRepository _tenants;
        private readonly IUsuarioRepository _usuarios;
        private readonly IHorarioFuncionamentoRepository _horarios;
        private readonly IRecursoRepository _recursos;
        private readonly IPasswordHasher _hasher;
        private readonly IUnitOfWork _uow;
        private readonly ITenantSeeder _seeder;
        private readonly Microsoft.Extensions.Logging.ILogger<CriarTenantUseCase> _logger;

        public CriarTenantUseCase(ITenantRepository tenants, IUsuarioRepository usuarios,
            IHorarioFuncionamentoRepository horarios, IRecursoRepository recursos,
            IPasswordHasher hasher, IUnitOfWork uow, ITenantSeeder seeder,
            Microsoft.Extensions.Logging.ILogger<CriarTenantUseCase> logger)
        {
            _tenants = tenants;
            _usuarios = usuarios;
            _horarios = horarios;
            _recursos = recursos;
            _hasher = hasher;
            _uow = uow;
            _seeder = seeder;
            _logger = logger;
        }

        public async Task<TenantViewModel> ExecuteAsync(CriarTenantInputModel input)
        {
            if (!await _tenants.SlugDisponivelAsync(input.Slug))
                throw new TenantException($"Slug '{input.Slug}' já está em uso.");

            Tenant tenant;
            int tenantId;

            await _uow.BeginTransactionAsync();
            try
            {
                tenant = new Tenant(input.Nome, input.Slug, input.Segmento, input.Email, input.Telefone);
                tenantId = await _tenants.CreateAsync(tenant);

                var senhaHash = _hasher.Hash(input.AdminSenha);
                var admin = new Usuario(tenantId, input.AdminNome, input.AdminEmail, senhaHash,
                    PerfilUsuario.Administrador, input.Telefone);
                await _usuarios.CreateAsync(admin);

                // Recurso default + horários padrão (segunda a sábado, 8 às 18h)
                var recurso = new Recurso(tenantId, "Box 01", "Recurso padrão", "Box", null, 1);
                await _recursos.CreateAsync(recurso);

                for (int d = 0; d < 7; d++)
                {
                    var dia = (DayOfWeek)d;
                    var aberto = dia != DayOfWeek.Sunday;
                    var horario = new HorarioFuncionamento(tenantId, dia,
                        new TimeSpan(8, 0, 0), new TimeSpan(18, 0, 0),
                        new TimeSpan(12, 0, 0), new TimeSpan(13, 0, 0), aberto);
                    await _horarios.CreateAsync(horario);
                }

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            // Dados de demonstração ficam FORA da transação do cadastro, e só quando
            // pedidos. O motivo é concreto: este seed já derrubou a criação de tenant
            // inteira uma vez, ao sortear dois agendamentos no mesmo horário. Dado
            // fictício não pode impedir o cadastro de um cliente de verdade — se
            // falhar, o tenant continua criado e utilizável, e o log conta o que houve.
            if (input.ComDadosDeExemplo)
            {
                try
                {
                    await _seeder.PopularAsync(tenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Tenant {TenantId} foi criado, mas os dados de exemplo falharam.",
                        tenantId);
                }
            }

            return MapearTenant(tenant);
        }

        public static TenantViewModel MapearTenant(Tenant t) => new()
        {
            Id = t.TenId,
            Nome = t.TenNome,
            Slug = t.TenSlug,
            Segmento = t.TenSegmento,
            Cnpj = t.TenCnpj,
            Email = t.TenEmail,
            Telefone = t.TenTelefone,
            WhatsApp = t.TenWhatsApp,
            Endereco = t.TenEndereco,
            Cidade = t.TenCidade,
            Estado = t.TenEstado,
            Cep = t.TenCep,
            Descricao = t.TenDescricao,
            Ativo = t.TenAtivo,
            Personalizacao = new PersonalizacaoViewModel
            {
                LogoUrl = t.TenLogoUrl,
                BannerUrl = t.TenBannerUrl,
                FaviconUrl = t.TenFaviconUrl,
                CorPrimaria = t.TenCorPrimaria,
                CorSecundaria = t.TenCorSecundaria,
                CorAcento = t.TenCorAcento,
                Fonte = t.TenFonte
            },
            Regras = new RegrasNegocioViewModel
            {
                PercentualEntrada = t.TenPercentualEntrada,
                BufferMinutos = t.TenBufferMinutos,
                AntecedenciaMinHoras = t.TenAntecedenciaMinHoras,
                AntecedenciaMaxDias = t.TenAntecedenciaMaxDias,
                LimiteCancelamentoHoras = t.TenLimiteCancelamentoHoras
            }
        };
    }

    public class ConsultarTenantUseCase : IConsultarTenantUseCase
    {
        private readonly ITenantRepository _tenants;
        public ConsultarTenantUseCase(ITenantRepository tenants) { _tenants = tenants; }

        public async Task<TenantViewModel> PorIdAsync(int id)
        {
            var t = await _tenants.GetByIdAsync(id);
            return t == null ? null : CriarTenantUseCase.MapearTenant(t);
        }

        public async Task<TenantViewModel> PorSlugAsync(string slug)
        {
            var t = await _tenants.GetBySlugAsync(slug);
            return t == null ? null : CriarTenantUseCase.MapearTenant(t);
        }

        public async Task<IEnumerable<TenantViewModel>> ListarTodosAsync()
        {
            var lista = await _tenants.GetAllAsync();
            return lista.Select(CriarTenantUseCase.MapearTenant);
        }
    }

    public class AtualizarTenantUseCase : IAtualizarTenantUseCase
    {
        private readonly ITenantRepository _tenants;
        private readonly IUnitOfWork _uow;

        public AtualizarTenantUseCase(ITenantRepository tenants, IUnitOfWork uow)
        {
            _tenants = tenants;
            _uow = uow;
        }

        public async Task<TenantViewModel> ExecuteAsync(int id, AtualizarTenantInputModel input)
        {
            var tenant = await _tenants.GetByIdAsync(id) ?? throw new TenantException("Tenant não encontrado.");
            tenant.Atualizar(input.Nome, input.Segmento, input.Cnpj, input.Email, input.Telefone,
                input.WhatsApp, input.Endereco, input.Cidade, input.Estado, input.Cep, input.Descricao);
            await _tenants.UpdateAsync(tenant);
            await _uow.SaveChangesAsync();
            return CriarTenantUseCase.MapearTenant(tenant);
        }

        public async Task<TenantViewModel> AtualizarPersonalizacaoAsync(int id, AtualizarPersonalizacaoInputModel input)
        {
            var tenant = await _tenants.GetByIdAsync(id) ?? throw new TenantException("Tenant não encontrado.");
            tenant.AtualizarPersonalizacao(input.LogoUrl, input.BannerUrl, input.FaviconUrl,
                input.CorPrimaria, input.CorSecundaria, input.CorAcento, input.Fonte);
            await _tenants.UpdateAsync(tenant);
            await _uow.SaveChangesAsync();
            return CriarTenantUseCase.MapearTenant(tenant);
        }

        public async Task<TenantViewModel> AtualizarRegrasAsync(int id, AtualizarRegrasNegocioInputModel input)
        {
            var tenant = await _tenants.GetByIdAsync(id) ?? throw new TenantException("Tenant não encontrado.");
            tenant.AtualizarRegrasNegocio(input.PercentualEntrada, input.BufferMinutos,
                input.AntecedenciaMinHoras, input.AntecedenciaMaxDias, input.LimiteCancelamentoHoras);
            await _tenants.UpdateAsync(tenant);
            await _uow.SaveChangesAsync();
            return CriarTenantUseCase.MapearTenant(tenant);
        }
    }
}

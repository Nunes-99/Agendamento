using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Application.Mappers;
using AgendamentoPro.Application.ViewModels.Assinaturas;
using AgendamentoPro.Core.Entities.Assinaturas;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Assinaturas
{
    public class SimularPagamentoAssinaturaUseCase : ISimularPagamentoAssinaturaUseCase
    {
        private readonly IAssinaturaRepository _assinaturas;
        private readonly IFaturaAssinaturaRepository _faturas;
        private readonly IUnitOfWork _uow;
        private readonly IAssinaturaCacheInvalidator _cache;

        public SimularPagamentoAssinaturaUseCase(IAssinaturaRepository a, IFaturaAssinaturaRepository f,
            IUnitOfWork uow, IAssinaturaCacheInvalidator cache)
        {
            _assinaturas = a; _faturas = f; _uow = uow; _cache = cache;
        }

        public async Task<AssinaturaViewModel> ExecuteAsync(int tenantId)
        {
            var ass = await _assinaturas.GetByTenantAsync(tenantId)
                ?? throw new DomainException("Tenant não possui assinatura ativa.");

            var agora = DateTime.UtcNow;
            var valor = ass.Plano?.PlnPreco ?? 29.90m;
            var refInicio = ass.AssUltimoPagamentoEm ?? ass.AssDataInicio;
            var proxVenc = agora.AddMonths(1);

            var fatura = new FaturaAssinatura(ass.R_TenId, ass.AssId, valor, refInicio, agora, agora);
            fatura.DefinirGatewayPaymentId($"dev-{Guid.NewGuid():N}", "{\"dev\":true}");
            fatura.Pagar(agora, "{\"dev\":\"simulado\"}");
            await _faturas.CreateAsync(fatura);

            ass.RegistrarPagamento(agora, proxVenc);
            await _assinaturas.UpdateAsync(ass);
            await _uow.SaveChangesAsync();
            _cache.Invalidar(tenantId);

            var atualizada = await _assinaturas.GetByIdAsync(ass.AssId);
            return AssinaturaMapper.ToViewModel(atualizada, await _faturas.ListarPorAssinaturaAsync(ass.AssId));
        }
    }

    public class ForcarStatusAssinaturaUseCase : IForcarStatusAssinaturaUseCase
    {
        private readonly IAssinaturaRepository _assinaturas;
        private readonly IFaturaAssinaturaRepository _faturas;
        private readonly IUnitOfWork _uow;
        private readonly IAssinaturaCacheInvalidator _cache;

        public ForcarStatusAssinaturaUseCase(IAssinaturaRepository a, IFaturaAssinaturaRepository f,
            IUnitOfWork uow, IAssinaturaCacheInvalidator cache)
        {
            _assinaturas = a; _faturas = f; _uow = uow; _cache = cache;
        }

        public async Task<AssinaturaViewModel> ExecuteAsync(int tenantId, StatusAssinatura novoStatus)
        {
            var ass = await _assinaturas.GetByTenantAsync(tenantId)
                ?? throw new DomainException("Tenant não possui assinatura.");

            AplicarTransicao(ass, novoStatus);
            await _assinaturas.UpdateAsync(ass);
            await _uow.SaveChangesAsync();
            _cache.Invalidar(tenantId);

            var atualizada = await _assinaturas.GetByIdAsync(ass.AssId);
            return AssinaturaMapper.ToViewModel(atualizada, await _faturas.ListarPorAssinaturaAsync(ass.AssId));
        }

        internal static void AplicarTransicao(Assinatura ass, StatusAssinatura alvo)
        {
            var agora = DateTime.UtcNow;
            switch (alvo)
            {
                case StatusAssinatura.Ativa:
                    ass.RegistrarPagamento(agora, agora.AddMonths(1));
                    break;
                case StatusAssinatura.Atrasada:
                    if (ass.AssStatus == StatusAssinatura.ReadOnly
                        || ass.AssStatus == StatusAssinatura.Cancelada
                        || ass.AssStatus == StatusAssinatura.Expirada)
                        ass.RegistrarPagamento(agora, agora.AddMonths(1));
                    ass.MarcarAtrasada(agora);
                    break;
                case StatusAssinatura.ReadOnly:
                    if (ass.AssStatus == StatusAssinatura.Cancelada
                        || ass.AssStatus == StatusAssinatura.Expirada)
                        ass.RegistrarPagamento(agora, agora.AddMonths(1));
                    if (ass.AssStatus != StatusAssinatura.Atrasada) ass.MarcarAtrasada(agora.AddDays(-9));
                    ass.TransicionarReadOnly(agora);
                    break;
                case StatusAssinatura.Expirada:
                    if (ass.AssStatus == StatusAssinatura.Cancelada)
                        ass.RegistrarPagamento(agora, agora.AddMonths(1));
                    if (ass.AssStatus != StatusAssinatura.Atrasada
                        && ass.AssStatus != StatusAssinatura.ReadOnly)
                        ass.MarcarAtrasada(agora.AddDays(-31));
                    if (ass.AssStatus != StatusAssinatura.ReadOnly)
                        ass.TransicionarReadOnly(agora.AddDays(-23));
                    ass.Expirar(agora);
                    break;
                case StatusAssinatura.Cancelada:
                    ass.Cancelar(agora);
                    break;
                case StatusAssinatura.Trial:
                    throw new DomainException("Status Trial só pode ser definido na criação da assinatura.");
            }
        }
    }

    public class SeedAssinaturasDemoUseCase : ISeedAssinaturasDemoUseCase
    {
        private static readonly StatusAssinatura[] StatusDemo =
        {
            StatusAssinatura.Ativa,
            StatusAssinatura.Atrasada,
            StatusAssinatura.ReadOnly,
            StatusAssinatura.Cancelada,
            StatusAssinatura.Expirada
        };

        private readonly ITenantRepository _tenants;
        private readonly IAssinaturaRepository _assinaturas;
        private readonly IPlanoRepository _planos;
        private readonly IUnitOfWork _uow;
        private readonly IAssinaturaCacheInvalidator _cache;

        public SeedAssinaturasDemoUseCase(ITenantRepository tenants, IAssinaturaRepository assinaturas,
            IPlanoRepository planos, IUnitOfWork uow, IAssinaturaCacheInvalidator cache)
        {
            _tenants = tenants; _assinaturas = assinaturas; _planos = planos;
            _uow = uow; _cache = cache;
        }

        public async Task<SeedAssinaturasResultViewModel> ExecuteAsync()
        {
            var plano = (await _planos.ListarPublicosAsync()).FirstOrDefault()
                ?? throw new DomainException("Nenhum plano cadastrado para seed.");

            var resultado = new SeedAssinaturasResultViewModel();

            foreach (var status in StatusDemo)
            {
                var slug = $"demo-{status.ToString().ToLowerInvariant()}";
                var existente = await _tenants.GetBySlugAsync(slug);
                if (existente != null)
                {
                    resultado.JaExistiam.Add(slug);
                    continue;
                }

                var tenant = new Tenant(
                    nome: $"Demo {status}",
                    slug: slug,
                    segmento: "demo",
                    email: $"{slug}@demo.local",
                    telefone: "+5511999999999");
                await _tenants.CreateAsync(tenant);

                var ass = new Assinatura(tenant.TenId, plano.PlnId, "MercadoPago");
                ass.DefinirPreapproval($"demo-pre-{tenant.TenId}", DateTime.UtcNow.AddDays(30));
                await _assinaturas.CreateAsync(ass);

                ForcarStatusAssinaturaUseCase.AplicarTransicao(ass, status);
                await _assinaturas.UpdateAsync(ass);
                _cache.Invalidar(tenant.TenId);

                resultado.Criadas.Add(new SeedItemViewModel { Slug = slug, Status = status });
            }

            await _uow.SaveChangesAsync();
            return resultado;
        }
    }
}

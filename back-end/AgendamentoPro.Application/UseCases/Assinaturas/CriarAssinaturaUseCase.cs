using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Application.InputModels.Assinaturas;
using AgendamentoPro.Application.Mappers;
using AgendamentoPro.Application.ViewModels.Assinaturas;
using AgendamentoPro.Core.Entities.Assinaturas;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace AgendamentoPro.Application.UseCases.Assinaturas
{
    public class CriarAssinaturaUseCase : ICriarAssinaturaUseCase
    {
        private readonly IAssinaturaRepository _assinaturas;
        private readonly IPlanoRepository _planos;
        private readonly IGatewayAssinatura _gateway;
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;
        private readonly IAssinaturaCacheInvalidator _cache;

        public CriarAssinaturaUseCase(IAssinaturaRepository assinaturas, IPlanoRepository planos,
            IGatewayAssinatura gateway, IUnitOfWork uow, IConfiguration config,
            IAssinaturaCacheInvalidator cache)
        {
            _assinaturas = assinaturas;
            _planos = planos;
            _gateway = gateway;
            _uow = uow;
            _config = config;
            _cache = cache;
        }

        public async Task<AssinaturaViewModel> ExecuteAsync(int tenantId, CriarAssinaturaInputModel input)
        {
            if (input == null) throw new DomainException("Dados da assinatura ausentes.");
            if (string.IsNullOrWhiteSpace(input.PayerEmail))
                throw new DomainException("E-mail do pagador é obrigatório.");

            var plano = await _planos.GetByIdAsync(input.PlanoId)
                ?? throw new DomainException("Plano inválido.");
            if (!plano.PlnAtivo) throw new DomainException("Plano não está ativo.");

            var atual = await _assinaturas.GetByTenantAsync(tenantId);
            if (atual != null && atual.AssStatus != StatusAssinatura.Cancelada
                              && atual.AssStatus != StatusAssinatura.Expirada)
                throw new DomainException("Tenant já possui assinatura ativa. Use alterar plano ou cancele a atual.");

            // Cria entity (status inicial = Ativa; será atualizado quando MP confirmar autorização do cartão)
            var assinatura = new Assinatura(tenantId, plano.PlnId, _gateway.Nome);
            await _assinaturas.CreateAsync(assinatura);

            // Cria preapproval no gateway
            var appUrl = (Environment.GetEnvironmentVariable("APP_PUBLIC_URL")
                ?? _config["App:PublicUrl"] ?? "http://localhost:5050").TrimEnd('/');
            var backUrl = $"{appUrl}/admin/minha-assinatura";

            var gwResult = await _gateway.CriarPreapprovalAsync(
                tenantId, assinatura.AssId, plano.PlnPreco,
                $"AgendamentoPro - {plano.PlnNome}", input.PayerEmail, backUrl);

            var proxVenc = gwResult.ProximoVencimento ?? DateTime.UtcNow.AddDays(30);
            assinatura.DefinirPreapproval(gwResult.PreapprovalId, proxVenc, gwResult.PayloadBruto);
            await _assinaturas.UpdateAsync(assinatura);
            await _uow.SaveChangesAsync();
            _cache.Invalidar(tenantId);

            // Anexa plano pro mapper (não veio do GetByTenant pois acabamos de criar)
            var resultado = await _assinaturas.GetByIdAsync(assinatura.AssId);
            return AssinaturaMapper.ToViewModel(resultado, null, gwResult.InitPointUrl);
        }
    }
}

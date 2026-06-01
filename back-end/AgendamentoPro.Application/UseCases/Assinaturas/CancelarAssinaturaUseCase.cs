using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Application.Mappers;
using AgendamentoPro.Application.ViewModels.Assinaturas;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Application.UseCases.Assinaturas
{
    public class CancelarAssinaturaUseCase : ICancelarAssinaturaUseCase
    {
        private readonly IAssinaturaRepository _assinaturas;
        private readonly IFaturaAssinaturaRepository _faturas;
        private readonly IGatewayAssinatura _gateway;
        private readonly IUnitOfWork _uow;
        private readonly IAssinaturaCacheInvalidator _cache;
        private readonly ILogger<CancelarAssinaturaUseCase> _logger;

        public CancelarAssinaturaUseCase(IAssinaturaRepository assinaturas, IFaturaAssinaturaRepository faturas,
            IGatewayAssinatura gateway, IUnitOfWork uow, IAssinaturaCacheInvalidator cache,
            ILogger<CancelarAssinaturaUseCase> logger)
        {
            _assinaturas = assinaturas;
            _faturas = faturas;
            _gateway = gateway;
            _uow = uow;
            _cache = cache;
            _logger = logger;
        }

        public async Task<AssinaturaViewModel> ExecuteAsync(int tenantId)
        {
            var ass = await _assinaturas.GetByTenantAsync(tenantId)
                ?? throw new DomainException("Tenant não possui assinatura ativa.");

            if (!string.IsNullOrEmpty(ass.AssGatewayPreapprovalId))
            {
                var ok = await _gateway.CancelarAsync(ass.AssGatewayPreapprovalId);
                if (!ok)
                    _logger.LogWarning("Cancelamento do preapproval {Id} falhou no gateway — marcando local mesmo assim.",
                        ass.AssGatewayPreapprovalId);
            }

            if (ass.Cancelar(DateTime.UtcNow))
            {
                await _assinaturas.UpdateAsync(ass);
                await _uow.SaveChangesAsync();
                _cache.Invalidar(tenantId);
                _logger.LogInformation("Tenant {Tid} cancelou assinatura {Ass}.", tenantId, ass.AssId);
            }

            return AssinaturaMapper.ToViewModel(ass, await _faturas.ListarPorAssinaturaAsync(ass.AssId));
        }
    }
}

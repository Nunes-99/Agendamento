using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Application.InputModels.Assinaturas;
using AgendamentoPro.Application.Mappers;
using AgendamentoPro.Application.ViewModels.Assinaturas;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Application.UseCases.Assinaturas
{
    public class AlterarPlanoUseCase : IAlterarPlanoUseCase
    {
        private readonly IAssinaturaRepository _assinaturas;
        private readonly IFaturaAssinaturaRepository _faturas;
        private readonly IPlanoRepository _planos;
        private readonly IGatewayAssinatura _gateway;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AlterarPlanoUseCase> _logger;

        public AlterarPlanoUseCase(IAssinaturaRepository assinaturas, IFaturaAssinaturaRepository faturas,
            IPlanoRepository planos, IGatewayAssinatura gateway, IUnitOfWork uow,
            ILogger<AlterarPlanoUseCase> logger)
        {
            _assinaturas = assinaturas;
            _faturas = faturas;
            _planos = planos;
            _gateway = gateway;
            _uow = uow;
            _logger = logger;
        }

        public async Task<AssinaturaViewModel> ExecuteAsync(int tenantId, AlterarPlanoInputModel input)
        {
            if (input == null) throw new DomainException("Dados ausentes.");

            var ass = await _assinaturas.GetByTenantAsync(tenantId)
                ?? throw new DomainException("Tenant não possui assinatura ativa.");

            var novoPlano = await _planos.GetByIdAsync(input.NovoPlanoId)
                ?? throw new DomainException("Plano destino inválido.");
            if (!novoPlano.PlnAtivo) throw new DomainException("Plano destino não está ativo.");

            if (ass.R_PlnId == novoPlano.PlnId)
                return AssinaturaMapper.ToViewModel(ass, await _faturas.ListarPorAssinaturaAsync(ass.AssId));

            // Atualiza valor no gateway antes de tocar no DB (rollback fácil se gateway falhar)
            if (!string.IsNullOrEmpty(ass.AssGatewayPreapprovalId))
            {
                var ok = await _gateway.AtualizarValorAsync(ass.AssGatewayPreapprovalId, novoPlano.PlnPreco);
                if (!ok)
                    throw new DomainException("Falha ao alterar valor no gateway. Tente novamente em instantes.");
            }

            ass.AlterarPlano(novoPlano.PlnId);
            await _assinaturas.UpdateAsync(ass);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Tenant {Tid} mudou plano para {Plano} (R$ {Preco:F2}).",
                tenantId, novoPlano.PlnNome, novoPlano.PlnPreco);

            var atualizada = await _assinaturas.GetByIdAsync(ass.AssId);
            return AssinaturaMapper.ToViewModel(atualizada,
                await _faturas.ListarPorAssinaturaAsync(ass.AssId));
        }
    }
}

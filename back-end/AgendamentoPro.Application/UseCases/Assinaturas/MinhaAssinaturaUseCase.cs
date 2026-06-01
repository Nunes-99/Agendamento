using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Application.Mappers;
using AgendamentoPro.Application.ViewModels.Assinaturas;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Assinaturas
{
    public class MinhaAssinaturaUseCase : IMinhaAssinaturaUseCase
    {
        private readonly IAssinaturaRepository _assinaturas;
        private readonly IFaturaAssinaturaRepository _faturas;

        public MinhaAssinaturaUseCase(IAssinaturaRepository assinaturas, IFaturaAssinaturaRepository faturas)
        {
            _assinaturas = assinaturas;
            _faturas = faturas;
        }

        public async Task<AssinaturaViewModel> ExecuteAsync(int tenantId)
        {
            var ass = await _assinaturas.GetByTenantAsync(tenantId);
            if (ass == null) return null;
            var faturas = await _faturas.ListarPorAssinaturaAsync(ass.AssId);
            return AssinaturaMapper.ToViewModel(ass, faturas);
        }
    }
}

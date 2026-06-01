using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Application.Mappers;
using AgendamentoPro.Application.ViewModels.Assinaturas;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Assinaturas
{
    public class ListarPlanosUseCase : IListarPlanosUseCase
    {
        private readonly IPlanoRepository _planos;

        public ListarPlanosUseCase(IPlanoRepository planos) { _planos = planos; }

        public async Task<IEnumerable<PlanoViewModel>> ExecuteAsync()
        {
            var planos = await _planos.ListarPublicosAsync();
            return planos.Select(AssinaturaMapper.ToViewModel);
        }
    }
}

using AgendamentoPro.Application.InputModels.Servicos;
using AgendamentoPro.Application.ViewModels.Servicos;

namespace AgendamentoPro.Application.Interfaces.Servicos
{
    public interface IComboUseCase
    {
        Task<ComboViewModel> CriarAsync(int tenantId, ComboInputModel input);
        Task<ComboViewModel> AtualizarAsync(int tenantId, int id, ComboInputModel input);
        Task RemoverAsync(int tenantId, int id);
        Task<ComboViewModel> ObterAsync(int tenantId, int id);
        Task<IEnumerable<ComboViewModel>> ListarAsync(int tenantId, bool somenteAtivos);
    }
}

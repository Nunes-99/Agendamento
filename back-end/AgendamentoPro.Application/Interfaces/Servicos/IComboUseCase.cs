using AgendamentoPro.Application.InputModels.Servicos;
using AgendamentoPro.Application.ViewModels.Agendamentos;
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

    public interface IAgendarComboUseCase
    {
        /// <summary>
        /// Cria N agendamentos contíguos (1 por serviço do combo) no mesmo recurso,
        /// com mesmo R_GrupoComboId, e gera 1 cobrança agregada vinculada ao primeiro.
        /// Quando o pagamento é aprovado via webhook, todos os agendamentos do grupo
        /// são confirmados em massa (consultar webhook handler).
        /// </summary>
        Task<AgendarComboResultViewModel> ExecuteAsync(int tenantId, int comboId, AgendarComboInputModel input);
    }

    public class AgendarComboResultViewModel
    {
        public Guid GrupoComboId { get; set; }
        public List<AgendamentoViewModel> Agendamentos { get; set; } = new();
        public PagamentoViewModel Pagamento { get; set; }
    }
}

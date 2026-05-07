using AgendamentoPro.Application.ViewModels.Relatorios;

namespace AgendamentoPro.Application.Interfaces.Relatorios
{
    public interface IRelatoriosUseCase
    {
        Task<IEnumerable<ReceitaPeriodoViewModel>> ReceitaPorDiaAsync(int tenantId, DateTime inicio, DateTime fim);
        Task<IEnumerable<ServicoMaisVendidoViewModel>> ServicosMaisVendidosAsync(int tenantId, DateTime inicio, DateTime fim);
        Task<IEnumerable<TaxaOcupacaoViewModel>> TaxaOcupacaoAsync(int tenantId, DateTime inicio, DateTime fim);
        Task<IEnumerable<CancelamentoViewModel>> CancelamentosAsync(int tenantId, DateTime inicio, DateTime fim);
    }
}

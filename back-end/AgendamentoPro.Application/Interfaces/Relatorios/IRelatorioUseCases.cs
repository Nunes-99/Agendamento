using AgendamentoPro.Application.ViewModels.Relatorios;

namespace AgendamentoPro.Application.Interfaces.Relatorios
{
    public interface IRelatoriosUseCase
    {
        Task<IEnumerable<ReceitaPeriodoViewModel>> ReceitaPorDiaAsync(int tenantId, DateTime inicio, DateTime fim);
        Task<IEnumerable<ServicoMaisVendidoViewModel>> ServicosMaisVendidosAsync(int tenantId, DateTime inicio, DateTime fim);
        Task<IEnumerable<TaxaOcupacaoViewModel>> TaxaOcupacaoAsync(int tenantId, DateTime inicio, DateTime fim);
        Task<IEnumerable<CancelamentoViewModel>> CancelamentosAsync(int tenantId, DateTime inicio, DateTime fim);

        /// <summary>Top N clientes por receita acumulada (default N=20).</summary>
        Task<IEnumerable<LtvClienteViewModel>> LtvClientesAsync(int tenantId, DateTime inicio, DateTime fim, int top = 20);

        /// <summary>Taxa de no-show agrupada por dia da semana (Segunda..Domingo).</summary>
        Task<IEnumerable<NoShowViewModel>> NoShowPorDiaSemanaAsync(int tenantId, DateTime inicio, DateTime fim);

        /// <summary>Taxa de no-show agrupada por hora do dia (0..23).</summary>
        Task<IEnumerable<NoShowViewModel>> NoShowPorHoraAsync(int tenantId, DateTime inicio, DateTime fim);

        /// <summary>Receita mensal nos últimos N meses (default 12).</summary>
        Task<IEnumerable<SazonalidadeMesViewModel>> SazonalidadeMensalAsync(int tenantId, int meses = 12);
    }
}

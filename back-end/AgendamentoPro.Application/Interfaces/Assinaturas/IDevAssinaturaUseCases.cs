using AgendamentoPro.Application.ViewModels.Assinaturas;
using AgendamentoPro.Core.Enums;

namespace AgendamentoPro.Application.Interfaces.Assinaturas
{
    /// <summary>
    /// Use cases de desenvolvimento — só registrados quando IHostEnvironment != Production.
    /// Permitem simular pagamentos e transições sem precisar de webhook real do MP.
    /// </summary>
    public interface ISimularPagamentoAssinaturaUseCase
    {
        Task<AssinaturaViewModel> ExecuteAsync(int tenantId);
    }

    public interface IForcarStatusAssinaturaUseCase
    {
        Task<AssinaturaViewModel> ExecuteAsync(int tenantId, StatusAssinatura novoStatus);
    }

    public interface ISeedAssinaturasDemoUseCase
    {
        Task<SeedAssinaturasResultViewModel> ExecuteAsync();
    }

    public class SeedAssinaturasResultViewModel
    {
        public List<SeedItemViewModel> Criadas { get; set; } = new();
        public List<string> JaExistiam { get; set; } = new();
    }

    public class SeedItemViewModel
    {
        public string Slug { get; set; }
        public StatusAssinatura Status { get; set; }
    }
}

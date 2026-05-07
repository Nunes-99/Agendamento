namespace AgendamentoPro.Application.Interfaces.Pagamentos
{
    public interface IProcessarWebhookPagamentoUseCase
    {
        Task ExecuteAsync(string gateway, string payload, string assinatura);
    }
}

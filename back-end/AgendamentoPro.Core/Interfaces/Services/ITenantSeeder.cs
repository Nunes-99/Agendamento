namespace AgendamentoPro.Core.Interfaces.Services
{
    /// <summary>
    /// Popula um tenant recém-criado com dados-exemplo (serviços, recursos, clientes, agendamentos).
    /// Implementação fica na Infrastructure para isolar a lógica de geração.
    /// </summary>
    public interface ITenantSeeder
    {
        Task PopularAsync(int tenantId);
    }
}

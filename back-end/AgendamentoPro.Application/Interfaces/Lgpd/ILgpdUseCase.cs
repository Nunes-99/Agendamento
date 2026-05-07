namespace AgendamentoPro.Application.Interfaces.Lgpd
{
    /// <summary>
    /// LGPD: portabilidade (exportação) e direito ao esquecimento (anonimização)
    /// dos dados pessoais de um cliente.
    /// </summary>
    public interface ILgpdUseCase
    {
        /// <summary>
        /// Retorna todos os dados pessoais associados ao cliente (cliente + agendamentos
        /// + avaliações + fotos URLs) num único objeto serializável.
        /// </summary>
        Task<object> ExportarDadosClienteAsync(int tenantId, int clienteId);

        /// <summary>
        /// Anonimiza o cliente: nome → "Cliente removido #N", e-mail → null,
        /// telefone/whatsapp → null, CPF → null. Mantém histórico de agendamentos
        /// (necessário para integridade contábil) mas sem identificação.
        /// </summary>
        Task AnonimizarClienteAsync(int tenantId, int clienteId);

        /// <summary>
        /// Anonimização em massa: clientes inativos há mais de N meses sem agendamentos
        /// são anonimizados automaticamente. Pensado pra rodar via Hangfire mensal.
        /// </summary>
        Task<int> AnonimizarInativosAsync(int tenantId, int inativoHaMeses);
    }
}

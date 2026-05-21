namespace AgendamentoPro.Core.Interfaces.Services
{
    /// <summary>
    /// Envia SMS. Usado como fallback quando WhatsApp falha (template não
    /// aprovado, número sem WhatsApp, janela expirada). Implementações:
    /// `TwilioSmsSender` (default) ou `NoOpSmsSender` quando não configurado.
    /// </summary>
    public interface ISmsSender
    {
        bool Ativo { get; }

        /// <summary>
        /// Envia SMS para o número (formato E.164 ou nacional — implementação normaliza).
        /// Retorna true se enviado com sucesso, false se a infra falhou (sem lançar).
        /// </summary>
        Task<bool> EnviarAsync(string numero, string mensagem, CancellationToken ct = default);
    }
}

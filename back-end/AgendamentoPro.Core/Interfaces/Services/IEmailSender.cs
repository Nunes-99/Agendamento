namespace AgendamentoPro.Core.Interfaces.Services
{
    /// <summary>
    /// Abstrai envio de e-mail. Implementação default (LogEmailSender) apenas loga
    /// o conteúdo (operador encaminha manualmente). Implementação SMTP plugável via
    /// SmtpEmailSender quando as variáveis SMTP_* estão configuradas.
    /// </summary>
    public interface IEmailSender
    {
        bool Ativo { get; }
        Task EnviarAsync(string para, string assunto, string corpoHtml, string corpoTexto = null,
            CancellationToken ct = default);
    }
}

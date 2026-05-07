using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace AgendamentoPro.Infrastructure.Services.Email
{
    /// <summary>
    /// Envio de e-mail via SMTP. Configuração via env vars:
    ///   SMTP_HOST, SMTP_PORT (default 587), SMTP_USERNAME, SMTP_PASSWORD,
    ///   SMTP_FROM_EMAIL, SMTP_FROM_NAME, SMTP_USE_SSL (default true)
    ///
    /// Se SMTP_HOST não estiver definido, .Ativo retorna false e EnviarAsync apenas loga
    /// (atua como fallback - operador entrega o conteúdo manualmente).
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly ILogger<SmtpEmailSender> _logger;
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _useSsl;

        public bool Ativo => !string.IsNullOrWhiteSpace(_host) && !string.IsNullOrWhiteSpace(_fromEmail);

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        {
            _logger = logger;
            _host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? config["Smtp:Host"] ?? "";
            _port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? config["Smtp:Port"], out var p) ? p : 587;
            _username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? config["Smtp:Username"] ?? "";
            _password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? config["Smtp:Password"] ?? "";
            _fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? config["Smtp:FromEmail"] ?? "";
            _fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? config["Smtp:FromName"] ?? "AgendamentoPro";
            _useSsl = (Environment.GetEnvironmentVariable("SMTP_USE_SSL") ?? config["Smtp:UseSsl"] ?? "true")
                .Equals("true", StringComparison.OrdinalIgnoreCase);

            if (!Ativo)
                _logger.LogWarning("SMTP não configurado. E-mails serão apenas logados.");
        }

        public async Task EnviarAsync(string para, string assunto, string corpoHtml, string corpoTexto = null,
            CancellationToken ct = default)
        {
            if (!Ativo)
            {
                _logger.LogInformation("[SMTP DESATIVADO] Para={Para}, Assunto={Assunto}, Corpo={Corpo}",
                    para, assunto, corpoTexto ?? corpoHtml);
                return;
            }

            using var msg = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = assunto,
                Body = corpoHtml,
                IsBodyHtml = true
            };
            msg.To.Add(para);
            if (!string.IsNullOrEmpty(corpoTexto))
            {
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                    corpoTexto, null, "text/plain"));
            }

            using var client = new SmtpClient(_host, _port)
            {
                EnableSsl = _useSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_username, _password),
                Timeout = 15000
            };

            try
            {
                await client.SendMailAsync(msg, ct);
                _logger.LogInformation("E-mail enviado para {Para}: {Assunto}", para, assunto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail para {Para}", para);
                // Não relança - email de reset não pode quebrar o fluxo (operador vai
                // entregar manualmente via log se SMTP estiver fora).
            }
        }
    }
}

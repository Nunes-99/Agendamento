using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AgendamentoPro.Infrastructure.Services.Sms
{
    /// <summary>
    /// Envia SMS via Twilio. No-op silencioso quando TWILIO_ACCOUNT_SID /
    /// TWILIO_AUTH_TOKEN / TWILIO_FROM_NUMBER não estão setados.
    ///
    /// Para usar Zenvia/Infobip no futuro, implemente ISmsSender separadamente
    /// e registre via DI selecionando por env SMS_PROVIDER.
    /// </summary>
    public class TwilioSmsSender : ISmsSender
    {
        private readonly ILogger<TwilioSmsSender> _logger;
        private readonly string _fromNumber;

        public TwilioSmsSender(IConfiguration config, ILogger<TwilioSmsSender> logger)
        {
            _logger = logger;
            var sid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID")
                ?? config["Twilio:AccountSid"];
            var token = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN")
                ?? config["Twilio:AuthToken"];
            _fromNumber = Environment.GetEnvironmentVariable("TWILIO_FROM_NUMBER")
                ?? config["Twilio:FromNumber"];

            if (!string.IsNullOrWhiteSpace(sid) && !string.IsNullOrWhiteSpace(token)
                && !string.IsNullOrWhiteSpace(_fromNumber))
            {
                TwilioClient.Init(sid, token);
                Ativo = true;
                _logger.LogInformation("TwilioSmsSender ativo (from={From}).", _fromNumber);
            }
            else
            {
                _logger.LogInformation(
                    "TwilioSmsSender no-op: TWILIO_ACCOUNT_SID/AUTH_TOKEN/FROM_NUMBER ausentes.");
            }
        }

        public bool Ativo { get; }

        public async Task<bool> EnviarAsync(string numero, string mensagem, CancellationToken ct = default)
        {
            if (!Ativo) return false;
            if (string.IsNullOrWhiteSpace(numero) || string.IsNullOrWhiteSpace(mensagem))
                return false;

            var destino = NormalizarE164(numero);
            try
            {
                var msg = await MessageResource.CreateAsync(
                    body: mensagem,
                    from: new PhoneNumber(_fromNumber),
                    to: new PhoneNumber(destino));

                if (msg.ErrorCode.HasValue)
                {
                    _logger.LogWarning("Twilio retornou erro {Code}: {Message}", msg.ErrorCode, msg.ErrorMessage);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao enviar SMS via Twilio para {Numero}", destino);
                return false;
            }
        }

        /// <summary>
        /// Normaliza um número BR para formato E.164 (+55DDNXXXXXXXX). Se o número já
        /// começa com '+', retorna como está; caso contrário assume BR (+55).
        /// </summary>
        public static string NormalizarE164(string numero)
        {
            var digitos = new string((numero ?? "").Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digitos)) return numero;
            if (numero.TrimStart().StartsWith("+")) return "+" + digitos;
            // Heurística BR: 10-11 dígitos (com DDD) → prefixa +55
            if (digitos.Length is 10 or 11) return "+55" + digitos;
            return "+" + digitos; // assume já tem código do país sem '+'
        }
    }
}

namespace AgendamentoPro.Core.Interfaces.Services
{
    /// <summary>
    /// Implementação RFC 6238 de TOTP. Compatível com Google Authenticator,
    /// Authy, 1Password etc. Janela de tolerância: ±1 step de 30s pra
    /// dessincronia leve de relógio.
    /// </summary>
    public interface ITotpService
    {
        /// <summary>Gera um secret base32 random de 20 bytes.</summary>
        string GerarSecret();

        /// <summary>URL otpauth:// para gerar QR Code de pareamento com app autenticador.</summary>
        string GerarOtpAuthUrl(string secretBase32, string emailUsuario, string emissor);

        /// <summary>Valida um código de 6 dígitos contra o secret.</summary>
        bool Verificar(string secretBase32, string codigo, DateTime agoraUtc);
    }
}

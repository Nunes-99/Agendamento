using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Usuarios
{
    /// <summary>
    /// Desafio OTP para autenticação de cliente final via WhatsApp.
    /// Códigos de 6 dígitos com validade de 10 minutos, máximo 3 tentativas.
    /// O código é armazenado como hash (BCrypt) — nunca em claro.
    /// </summary>
    public class OtpChallenge : ITenantScoped
    {
        public int OtpId { get; private set; }
        public int R_TenId { get; private set; }
        public string OtpTelefone { get; private set; }
        public string OtpCodigoHash { get; private set; }
        public DateTime OtpCriadoEm { get; private set; }
        public DateTime OtpExpiraEm { get; private set; }
        public int OtpTentativas { get; private set; }
        public bool OtpUsado { get; private set; }

        protected OtpChallenge() { }

        public OtpChallenge(int rTenId, string telefone, string codigoHash, TimeSpan validade)
        {
            R_TenId = rTenId;
            OtpTelefone = (telefone ?? string.Empty).Trim();
            OtpCodigoHash = codigoHash;
            OtpCriadoEm = DateTime.UtcNow;
            OtpExpiraEm = DateTime.UtcNow.Add(validade);
            OtpTentativas = 0;
            OtpUsado = false;
        }

        public bool Expirou(DateTime agora) => agora >= OtpExpiraEm;
        public bool ExcedeuTentativas() => OtpTentativas >= 3;
        public bool Disponivel(DateTime agora) => !OtpUsado && !Expirou(agora) && !ExcedeuTentativas();

        public void RegistrarFalha() => OtpTentativas++;
        public void MarcarUsado() => OtpUsado = true;
    }
}

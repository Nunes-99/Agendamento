using AgendamentoPro.Core.Exceptions;

namespace AgendamentoPro.Core.Entities.Usuarios
{
    /// <summary>
    /// Token de redefinição de senha. Validade curta (1h por default), uso único.
    /// </summary>
    public class PasswordReset
    {
        public int RpsId { get; private set; }
        public int R_UsuId { get; private set; }
        public string RpsToken { get; private set; }
        public DateTime RpsExpiraEm { get; private set; }
        public bool RpsUsado { get; private set; }
        public DateTime RpsCriadoEm { get; private set; }
        public DateTime? RpsUsadoEm { get; private set; }

        public Usuario Usuario { get; private set; }

        protected PasswordReset() { }

        public PasswordReset(int rUsuId, string token, TimeSpan validade)
        {
            if (rUsuId <= 0) throw new DomainException("Usuário é obrigatório.");
            if (string.IsNullOrWhiteSpace(token)) throw new DomainException("Token é obrigatório.");
            R_UsuId = rUsuId;
            RpsToken = token;
            RpsCriadoEm = DateTime.UtcNow;
            RpsExpiraEm = RpsCriadoEm.Add(validade);
            RpsUsado = false;
        }

        public bool EstaValido(DateTime agoraUtc) =>
            !RpsUsado && agoraUtc < RpsExpiraEm;

        public void MarcarUsado()
        {
            if (RpsUsado) throw new DomainException("Token já foi utilizado.");
            RpsUsado = true;
            RpsUsadoEm = DateTime.UtcNow;
        }
    }
}

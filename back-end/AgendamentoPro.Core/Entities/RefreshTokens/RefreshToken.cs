using AgendamentoPro.Core.Entities.Usuarios;

namespace AgendamentoPro.Core.Entities.RefreshTokens
{
    public class RefreshToken
    {
        public int RefId { get; private set; }
        public int R_UsuId { get; private set; }
        public string RefToken { get; private set; }
        public string RefJwtId { get; private set; }
        public bool RefUtilizado { get; private set; }
        public bool RefRevogado { get; private set; }
        public DateTime RefExpiracao { get; private set; }
        public DateTime RefCriadoEm { get; private set; }

        public Usuario Usuario { get; private set; }

        protected RefreshToken() { }

        public RefreshToken(int rUsuId, string token, string jwtId, DateTime expiracao)
        {
            R_UsuId = rUsuId;
            RefToken = token;
            RefJwtId = jwtId;
            RefExpiracao = expiracao;
            RefCriadoEm = DateTime.UtcNow;
        }

        public void MarcarUtilizado() => RefUtilizado = true;
        public void Revogar() => RefRevogado = true;
        public bool EstaValido() => !RefUtilizado && !RefRevogado && RefExpiracao > DateTime.UtcNow;
    }
}

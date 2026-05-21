using AgendamentoPro.Core.Entities.Common;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;

namespace AgendamentoPro.Core.Entities.Usuarios
{
    /// <summary>
    /// Usuário do sistema (admin, atendente, super-admin).
    /// SuperAdmin tem R_TenId nulo (acesso global ao SaaS).
    /// </summary>
    public class Usuario : SoftDeletableEntity
    {
        public int UsuId { get; private set; }
        public int? R_TenId { get; private set; }
        public string UsuNome { get; private set; }
        public string UsuEmail { get; private set; }
        public string UsuSenha { get; private set; }
        public string UsuPerfil { get; private set; }
        public string UsuTelefone { get; private set; }
        public bool UsuAtivo { get; private set; }
        public DateTime? UsuUltimoLogin { get; private set; }
        public DateTime UsuCriadoEm { get; private set; }
        public int UsuTentativasFalhas { get; private set; }
        public DateTime? UsuBloqueadoAte { get; private set; }
        public string UsuTotpSecret { get; private set; }
        public bool UsuTotpAtivo { get; private set; }
        /// <summary>
        /// Última step TOTP (30s buckets desde epoch) que foi aceita com sucesso.
        /// Impede replay: o mesmo código não pode ser usado 2x. RFC 6238 §5.2.
        /// </summary>
        public long? UsuTotpUltimoStep { get; private set; }

        public Tenant Tenant { get; private set; }

        protected Usuario() { }

        public Usuario(int? rTenId, string nome, string email, string senhaHash, string perfil, string telefone)
        {
            R_TenId = rTenId;
            UsuNome = nome;
            UsuEmail = (email ?? string.Empty).ToLowerInvariant().Trim();
            UsuSenha = senhaHash;
            UsuPerfil = perfil;
            UsuTelefone = telefone;
            UsuAtivo = true;
            UsuCriadoEm = DateTime.UtcNow;
            Validate();
        }

        public void Atualizar(string nome, string email, string telefone, string perfil)
        {
            UsuNome = nome;
            UsuEmail = (email ?? string.Empty).ToLowerInvariant().Trim();
            UsuTelefone = telefone;
            UsuPerfil = perfil;
            Validate();
        }

        public void AlterarSenha(string novoHash)
        {
            UsuSenha = novoHash;
            UsuTentativasFalhas = 0;
            UsuBloqueadoAte = null;
        }
        public void RegistrarLogin()
        {
            UsuUltimoLogin = DateTime.UtcNow;
            UsuTentativasFalhas = 0;
            UsuBloqueadoAte = null;
        }
        public void RegistrarFalhaLogin(int tentativasMax, TimeSpan duracaoBloqueio)
        {
            UsuTentativasFalhas++;
            if (UsuTentativasFalhas >= tentativasMax)
                UsuBloqueadoAte = DateTime.UtcNow.Add(duracaoBloqueio);
        }
        public bool EstaBloqueado(DateTime agoraUtc) =>
            UsuBloqueadoAte.HasValue && agoraUtc < UsuBloqueadoAte.Value;
        /// <summary>
        /// Salva o secret pendente de confirmação. Não ativa 2FA — para isso é preciso
        /// chamar AtivarTotp() depois que o usuário confirmar com um código válido.
        /// </summary>
        public void DefinirTotpSecret(string base32Secret)
        {
            UsuTotpSecret = base32Secret;
            // Não toca em UsuTotpAtivo — só Ativar/Desativar fazem isso explicitamente.
        }
        public void AtivarTotp() => UsuTotpAtivo = !string.IsNullOrEmpty(UsuTotpSecret);
        public void DesativarTotp()
        {
            UsuTotpSecret = null;
            UsuTotpAtivo = false;
            UsuTotpUltimoStep = null;
        }

        /// <summary>
        /// Registra a step TOTP usada. Idempotente: chamada com step menor é no-op.
        /// Retorna true se o registro foi feito, false se a step é antiga (replay).
        /// </summary>
        public bool RegistrarTotpStep(long step)
        {
            if (UsuTotpUltimoStep.HasValue && step <= UsuTotpUltimoStep.Value)
                return false;
            UsuTotpUltimoStep = step;
            return true;
        }
        public void Ativar() => UsuAtivo = true;
        public void Inativar() => UsuAtivo = false;

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(UsuNome))
                throw new UsuarioException("Nome é obrigatório.");
            if (string.IsNullOrWhiteSpace(UsuEmail))
                throw new UsuarioException("Email é obrigatório.");
            if (string.IsNullOrWhiteSpace(UsuPerfil))
                throw new UsuarioException("Perfil é obrigatório.");
        }
    }
}

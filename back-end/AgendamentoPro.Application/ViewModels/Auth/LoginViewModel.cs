namespace AgendamentoPro.Application.ViewModels.Auth
{
    public class LoginViewModel
    {
        public int UsuId { get; set; }
        public int? TenantId { get; set; }
        public string TenantSlug { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Perfil { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expiracao { get; set; }
        /// <summary>Quando true, o login parou na etapa de senha e precisa de TOTP.</summary>
        public bool RequerTotp { get; set; }
        /// <summary>Mensagem amigável - usado quando login é negado (bloqueado, etc).</summary>
        public string Mensagem { get; set; }
    }
}

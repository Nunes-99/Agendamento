namespace AgendamentoPro.Application.InputModels.Auth
{
    public class LoginInputModel
    {
        public string Email { get; set; }
        public string Senha { get; set; }
        public string TenantSlug { get; set; }
    }
}

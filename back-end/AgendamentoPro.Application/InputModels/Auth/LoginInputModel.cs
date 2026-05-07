namespace AgendamentoPro.Application.InputModels.Auth
{
    public class LoginInputModel
    {
        public string Email { get; set; }
        public string Senha { get; set; }
        public string TenantSlug { get; set; }
        /// <summary>Código TOTP de 6 dígitos quando 2FA está ativo no usuário.</summary>
        public string CodigoTotp { get; set; }
        /// <summary>Token reCAPTCHA v3. Opcional — só é validado quando RECAPTCHA_SECRET_KEY está configurado.</summary>
        public string RecaptchaToken { get; set; }
    }
}

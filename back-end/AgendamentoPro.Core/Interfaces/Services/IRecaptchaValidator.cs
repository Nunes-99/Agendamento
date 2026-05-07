namespace AgendamentoPro.Core.Interfaces.Services
{
    /// <summary>
    /// Validador do Google reCAPTCHA v3. Quando RECAPTCHA_SECRET_KEY não está
    /// configurado, sempre retorna true (modo no-op para dev).
    /// Score mínimo recomendado: 0.5 (default). Acima disso = humano provável.
    /// </summary>
    public interface IRecaptchaValidator
    {
        bool Ativo { get; }
        Task<bool> ValidarAsync(string token, string acao, double scoreMinimo = 0.5);
    }
}

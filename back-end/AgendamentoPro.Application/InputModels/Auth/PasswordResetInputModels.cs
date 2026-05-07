namespace AgendamentoPro.Application.InputModels.Auth
{
    public class SolicitarResetSenhaInputModel
    {
        public string Email { get; set; }
    }

    public class RedefinirSenhaInputModel
    {
        public string Token { get; set; }
        public string NovaSenha { get; set; }
    }
}

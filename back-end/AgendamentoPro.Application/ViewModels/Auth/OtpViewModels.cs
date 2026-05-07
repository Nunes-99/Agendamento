namespace AgendamentoPro.Application.ViewModels.Auth
{
    public class SolicitarOtpResultViewModel
    {
        public bool Enviado { get; set; }
        /// <summary>Em desenvolvimento, retorna o código gerado pra facilitar testes (nunca em produção).</summary>
        public string CodigoDev { get; set; }
        public DateTime ExpiraEm { get; set; }
        public int CooldownSegundos { get; set; }
    }

    public class ValidarOtpResultViewModel
    {
        public bool Valido { get; set; }
        public string Token { get; set; }
        public DateTime Expiracao { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNome { get; set; }
        public string Mensagem { get; set; }
    }
}

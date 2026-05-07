namespace AgendamentoPro.Application.InputModels.Auth
{
    public class SolicitarOtpInputModel
    {
        public string Telefone { get; set; }
    }

    public class ValidarOtpInputModel
    {
        public string Telefone { get; set; }
        public string Codigo { get; set; }
    }
}

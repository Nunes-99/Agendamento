using AgendamentoPro.Application.InputModels.Auth;

namespace AgendamentoPro.Application.Interfaces.Auth
{
    public interface ISolicitarResetSenhaUseCase
    {
        /// <summary>
        /// Cria token de reset e retorna URL para o operador entregar (email/whatsapp).
        /// Sempre retorna sem revelar se o email existe ou não, mas o link só é gerado
        /// quando há usuário válido.
        /// </summary>
        Task<SolicitarResetSenhaResultViewModel> ExecuteAsync(SolicitarResetSenhaInputModel input);
    }

    public class SolicitarResetSenhaResultViewModel
    {
        /// <summary>True se um link foi gerado (não revelar pra requester em produção).</summary>
        public bool LinkGerado { get; set; }
        /// <summary>URL completa pra colar no WhatsApp/email. Sempre logada; em prod só visível pelo operador.</summary>
        public string LinkReset { get; set; }
        public DateTime? ExpiraEm { get; set; }
    }

    public interface IRedefinirSenhaUseCase
    {
        Task ExecuteAsync(RedefinirSenhaInputModel input);
    }
}

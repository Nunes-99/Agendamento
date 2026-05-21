using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Notificacoes
{
    public class Notificacao : ITenantScoped
    {
        public int NotId { get; private set; }
        public int R_TenId { get; private set; }
        public int? R_AgeId { get; private set; }
        public string NotCanal { get; private set; }
        public string NotTipo { get; private set; }
        public string NotDestinatario { get; private set; }
        public string NotMensagem { get; private set; }
        public string NotStatus { get; private set; }
        public string NotErro { get; private set; }
        public DateTime NotCriadoEm { get; private set; }
        public DateTime? NotEnviadoEm { get; private set; }

        public Tenant Tenant { get; private set; }

        protected Notificacao() { }

        public Notificacao(int rTenId, int? rAgeId, string canal, string tipo,
            string destinatario, string mensagem)
        {
            R_TenId = rTenId;
            R_AgeId = rAgeId;
            NotCanal = canal;
            NotTipo = tipo;
            NotDestinatario = destinatario;
            NotMensagem = mensagem;
            NotStatus = "Pendente";
            NotCriadoEm = DateTime.UtcNow;
        }

        public void MarcarEnviada()
        {
            NotStatus = "Enviada";
            NotEnviadoEm = DateTime.UtcNow;
        }

        public void MarcarErro(string erro)
        {
            NotStatus = "Erro";
            NotErro = erro;
        }

        /// <summary>Usado quando o canal original falha e o envio cai pra um fallback (ex: WhatsApp → SMS).</summary>
        public void AlterarCanal(string novoCanal) => NotCanal = novoCanal;
    }
}

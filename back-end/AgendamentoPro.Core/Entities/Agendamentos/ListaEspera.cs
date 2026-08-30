using AgendamentoPro.Core.Common;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Agendamentos
{
    /// <summary>
    /// Cliente entra em espera quando todos os slots de uma data estão ocupados.
    /// Quando alguém cancela, admin pode notificar os primeiros da fila.
    /// </summary>
    public class ListaEspera : ITenantScoped
    {
        public int LesId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_SerId { get; private set; }
        public DateTime LesDataDesejada { get; private set; }
        public string LesClienteNome { get; private set; }
        public string LesClienteTelefone { get; private set; }
        public string LesClienteEmail { get; private set; }
        public string LesObservacao { get; private set; }
        public bool LesNotificado { get; private set; }
        public DateTime? LesNotificadoEm { get; private set; }
        public DateTime LesCriadoEm { get; private set; }

        public Tenant Tenant { get; private set; }
        public Servico Servico { get; private set; }

        protected ListaEspera() { }

        public ListaEspera(int rTenId, int rSerId, DateTime dataDesejada,
            string nome, string telefone, string email, string observacao)
        {
            Exception Erro(string msg) => new DomainException(msg);

            if (rTenId <= 0) throw new DomainException("Tenant é obrigatório.");
            if (rSerId <= 0) throw new DomainException("Serviço é obrigatório.");

            R_TenId = rTenId;
            R_SerId = rSerId;
            LesDataDesejada = dataDesejada.Date;
            LesClienteNome = CampoTexto.Obrigatorio(nome, 200, "Nome", Erro);
            LesClienteTelefone = CampoTexto.Telefone(telefone, "Telefone", Erro);
            LesClienteEmail = CampoTexto.Email(email, "E-mail", Erro);
            LesObservacao = CampoTexto.Opcional(observacao, 500, "Observação", Erro);

            if (LesClienteTelefone == null && LesClienteEmail == null)
                throw new DomainException("Informe telefone ou e-mail para contato.");
            LesNotificado = false;
            LesCriadoEm = DateTime.UtcNow;
        }

        public void MarcarNotificado()
        {
            LesNotificado = true;
            LesNotificadoEm = DateTime.UtcNow;
        }
    }
}

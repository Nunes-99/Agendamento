using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Agendamentos
{
    /// <summary>
    /// Avaliação do serviço pelo cliente (1 a 5 estrelas + comentário opcional).
    /// O cliente acessa via token público (sem login) gerado quando o agendamento é concluído.
    /// </summary>
    public class Avaliacao : ITenantScoped
    {
        public int AvaId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_AgeId { get; private set; }
        public int R_CliId { get; private set; }
        public Guid AvaToken { get; private set; }
        public int? AvaNota { get; private set; }
        public string AvaComentario { get; private set; }
        public DateTime AvaCriadoEm { get; private set; }
        public DateTime? AvaRespondidoEm { get; private set; }
        public bool AvaPublica { get; private set; }

        public Tenant Tenant { get; private set; }
        public Agendamento Agendamento { get; private set; }
        public Cliente Cliente { get; private set; }

        protected Avaliacao() { }

        public Avaliacao(int rTenId, int rAgeId, int rCliId)
        {
            if (rTenId <= 0) throw new DomainException("Tenant é obrigatório.");
            if (rAgeId <= 0) throw new DomainException("Agendamento é obrigatório.");
            if (rCliId <= 0) throw new DomainException("Cliente é obrigatório.");

            R_TenId = rTenId;
            R_AgeId = rAgeId;
            R_CliId = rCliId;
            AvaToken = Guid.NewGuid();
            AvaCriadoEm = DateTime.UtcNow;
            AvaPublica = true;
        }

        public void Responder(int nota, string comentario)
        {
            if (AvaRespondidoEm.HasValue)
                throw new DomainException("Avaliação já foi respondida.");
            if (nota < 1 || nota > 5)
                throw new DomainException("Nota deve estar entre 1 e 5.");
            AvaNota = nota;
            AvaComentario = (comentario ?? string.Empty).Length > 1000
                ? comentario![..1000] : comentario;
            AvaRespondidoEm = DateTime.UtcNow;
        }

        public void DefinirVisibilidade(bool publica)
        {
            AvaPublica = publica;
        }
    }
}

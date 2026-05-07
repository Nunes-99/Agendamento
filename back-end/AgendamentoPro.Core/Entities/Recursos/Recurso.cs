using AgendamentoPro.Core.Entities.Common;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;
// DomainException usado abaixo

namespace AgendamentoPro.Core.Entities.Recursos
{
    /// <summary>
    /// Recurso de atendimento (box, sala, profissional, equipamento) que executa o serviço.
    /// Genérico: pode representar uma cabine de lava-rápido, uma cadeira de barbearia,
    /// uma sala de massagem ou um profissional.
    /// </summary>
    public class Recurso : SoftDeletableEntity, ITenantScoped
    {
        public int RecId { get; private set; }
        public int R_TenId { get; private set; }
        public string RecNome { get; private set; }
        public string RecDescricao { get; private set; }
        public string RecTipo { get; private set; }
        public string RecImagemUrl { get; private set; }
        public bool RecAtivo { get; private set; }
        public int RecOrdem { get; private set; }
        public DateTime RecCriadoEm { get; private set; }

        public Tenant Tenant { get; private set; }

        protected Recurso() { }

        public Recurso(int rTenId, string nome, string descricao, string tipo, string imagemUrl, int ordem)
        {
            R_TenId = rTenId;
            RecNome = nome;
            RecDescricao = descricao;
            RecTipo = tipo;
            RecImagemUrl = imagemUrl;
            RecOrdem = ordem;
            RecAtivo = true;
            RecCriadoEm = DateTime.UtcNow;
            Validate();
        }

        public void Atualizar(string nome, string descricao, string tipo, string imagemUrl, int ordem)
        {
            RecNome = nome;
            RecDescricao = descricao;
            RecTipo = tipo;
            RecImagemUrl = imagemUrl;
            RecOrdem = ordem;
            Validate();
        }

        public void Ativar() => RecAtivo = true;
        public void Inativar() => RecAtivo = false;

        private void Validate()
        {
            if (R_TenId <= 0)
                throw new DomainException("Tenant é obrigatório.");
            if (string.IsNullOrWhiteSpace(RecNome))
                throw new DomainException("Nome do recurso é obrigatório.");
        }
    }
}

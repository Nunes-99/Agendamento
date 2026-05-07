using AgendamentoPro.Core.Entities.Common;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Clientes
{
    public class Cliente : SoftDeletableEntity, ITenantScoped
    {
        public int CliId { get; private set; }
        public int R_TenId { get; private set; }
        public string CliNome { get; private set; }
        public string CliEmail { get; private set; }
        public string CliTelefone { get; private set; }
        public string CliWhatsApp { get; private set; }
        public string CliCpf { get; private set; }
        public string CliObservacao { get; private set; }
        public DateTime CliCriadoEm { get; private set; }

        public Tenant Tenant { get; private set; }

        protected Cliente() { }

        public Cliente(int rTenId, string nome, string email, string telefone, string whatsapp, string cpf, string observacao = null)
        {
            R_TenId = rTenId;
            CliNome = nome;
            CliEmail = email;
            CliTelefone = telefone;
            CliWhatsApp = whatsapp;
            CliCpf = cpf;
            CliObservacao = observacao;
            CliCriadoEm = DateTime.UtcNow;
            Validate();
        }

        public void Atualizar(string nome, string email, string telefone, string whatsapp, string cpf, string observacao)
        {
            CliNome = nome;
            CliEmail = email;
            CliTelefone = telefone;
            CliWhatsApp = whatsapp;
            CliCpf = cpf;
            CliObservacao = observacao;
            Validate();
        }

        private void Validate()
        {
            if (R_TenId <= 0)
                throw new ClienteException("Tenant é obrigatório.");
            if (string.IsNullOrWhiteSpace(CliNome))
                throw new ClienteException("Nome é obrigatório.");
            if (string.IsNullOrWhiteSpace(CliTelefone) && string.IsNullOrWhiteSpace(CliWhatsApp))
                throw new ClienteException("Telefone ou WhatsApp é obrigatório.");
        }
    }
}

using AgendamentoPro.Core.Common;
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
            CliCriadoEm = DateTime.UtcNow;
            Preencher(nome, email, telefone, whatsapp, cpf, observacao);
        }

        public void Atualizar(string nome, string email, string telefone, string whatsapp, string cpf, string observacao)
            => Preencher(nome, email, telefone, whatsapp, cpf, observacao);

        /// <summary>
        /// Sanea e valida antes de gravar. Os limites espelham as colunas do banco
        /// (ver AgendamentoProDbContext): o SQLite não impõe HasMaxLength, então
        /// sem esta checagem o formulário público aceitaria texto de qualquer
        /// tamanho — e um e-mail malformado só estourava lá na cobrança.
        /// </summary>
        private void Preencher(string nome, string email, string telefone, string whatsapp,
            string cpf, string observacao)
        {
            Exception Erro(string msg) => new ClienteException(msg);

            CliNome = CampoTexto.Obrigatorio(nome, 200, "Nome", Erro);
            CliEmail = CampoTexto.Email(email, "E-mail", Erro);
            CliTelefone = CampoTexto.Telefone(telefone, "Telefone", Erro);
            CliWhatsApp = CampoTexto.Telefone(whatsapp, "WhatsApp", Erro);
            CliCpf = CampoTexto.Cpf(cpf, "CPF", Erro);
            CliObservacao = CampoTexto.Opcional(observacao, 1000, "Observação", Erro);
            Validate();
        }

        /// <summary>
        /// Guarda o telefone só com dígitos (sem DDI 55 quando redundante). Cada
        /// fluxo mandava uma máscara diferente — "(11) 99887-7665" no agendamento,
        /// "11998877665" no OTP — e o mesmo cliente virava dois cadastros. A busca
        /// já normaliza os dois lados; gravar canônico deixa o dado consistente
        /// daqui pra frente e simplifica futuras integrações (WhatsApp/SMS).
        /// </summary>
        public static string NormalizarTelefone(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return valor;
            var digitos = new string(valor.Where(char.IsDigit).ToArray());
            if (digitos.Length == 0) return valor.Trim();
            // 5511999998888 -> 11999998888 (DDI só atrapalha a comparação local)
            if (digitos.Length > 11 && digitos.StartsWith("55")) digitos = digitos[2..];
            return digitos;
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

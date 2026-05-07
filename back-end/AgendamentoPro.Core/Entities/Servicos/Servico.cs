using AgendamentoPro.Core.Entities.Common;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Servicos
{
    public class Servico : SoftDeletableEntity, ITenantScoped
    {
        public int SerId { get; private set; }
        public int R_TenId { get; private set; }
        public string SerNome { get; private set; }
        public string SerDescricao { get; private set; }
        public decimal SerPreco { get; private set; }
        public int SerDuracaoMinutos { get; private set; }
        public string SerImagemUrl { get; private set; }
        public string SerCategoria { get; private set; }
        public bool SerAtivo { get; private set; }
        public int SerOrdem { get; private set; }
        public DateTime SerCriadoEm { get; private set; }

        public Tenant Tenant { get; private set; }

        protected Servico() { }

        public Servico(int rTenId, string nome, string descricao, decimal preco,
            int duracaoMinutos, string imagemUrl, string categoria, int ordem)
        {
            R_TenId = rTenId;
            SerNome = nome;
            SerDescricao = descricao;
            SerPreco = preco;
            SerDuracaoMinutos = duracaoMinutos;
            SerImagemUrl = imagemUrl;
            SerCategoria = categoria;
            SerOrdem = ordem;
            SerAtivo = true;
            SerCriadoEm = DateTime.UtcNow;
            Validate();
        }

        public void Atualizar(string nome, string descricao, decimal preco,
            int duracaoMinutos, string imagemUrl, string categoria, int ordem)
        {
            SerNome = nome;
            SerDescricao = descricao;
            SerPreco = preco;
            SerDuracaoMinutos = duracaoMinutos;
            SerImagemUrl = imagemUrl;
            SerCategoria = categoria;
            SerOrdem = ordem;
            Validate();
        }

        public void Ativar() => SerAtivo = true;
        public void Inativar() => SerAtivo = false;

        private void Validate()
        {
            if (R_TenId <= 0)
                throw new ServicoException("Tenant é obrigatório.");
            if (string.IsNullOrWhiteSpace(SerNome))
                throw new ServicoException("Nome do serviço é obrigatório.");
            if (SerNome.Length > 150)
                throw new ServicoException("Nome deve ter no máximo 150 caracteres.");
            if (SerPreco < 0)
                throw new ServicoException("Preço não pode ser negativo.");
            if (SerDuracaoMinutos <= 0)
                throw new ServicoException("Duração deve ser maior que zero.");
        }
    }
}

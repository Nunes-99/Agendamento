using AgendamentoPro.Core.Entities.Common;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Servicos
{
    /// <summary>
    /// Combo: agrupamento promocional de N serviços com preço final reduzido.
    /// Visível no catálogo público e usado como upsell quando o cliente seleciona
    /// um dos serviços que compõe o combo.
    /// </summary>
    public class Combo : SoftDeletableEntity, ITenantScoped
    {
        public int ComId { get; private set; }
        public int R_TenId { get; private set; }
        public string ComNome { get; private set; }
        public string ComDescricao { get; private set; }
        public string ComImagemUrl { get; private set; }
        public decimal ComPrecoPromocional { get; private set; }
        public bool ComAtivo { get; private set; }
        public int ComOrdem { get; private set; }
        public DateTime ComCriadoEm { get; private set; }

        public Tenant Tenant { get; private set; }
        public List<ComboServico> Servicos { get; private set; }

        protected Combo() { Servicos = new List<ComboServico>(); }

        public Combo(int rTenId, string nome, string descricao, string imagemUrl,
            decimal precoPromocional, int ordem)
        {
            Servicos = new List<ComboServico>();
            R_TenId = rTenId;
            ComNome = nome;
            ComDescricao = descricao;
            ComImagemUrl = imagemUrl;
            ComPrecoPromocional = precoPromocional;
            ComOrdem = ordem;
            ComAtivo = true;
            ComCriadoEm = DateTime.UtcNow;
            Validate();
        }

        public void Atualizar(string nome, string descricao, string imagemUrl,
            decimal precoPromocional, int ordem, bool ativo)
        {
            ComNome = nome;
            ComDescricao = descricao;
            ComImagemUrl = imagemUrl;
            ComPrecoPromocional = precoPromocional;
            ComOrdem = ordem;
            ComAtivo = ativo;
            Validate();
        }

        public void DefinirServicos(IEnumerable<int> servicoIds)
        {
            Servicos.Clear();
            foreach (var sid in servicoIds.Distinct())
                Servicos.Add(new ComboServico(ComId, sid));
        }

        private void Validate()
        {
            if (R_TenId <= 0) throw new ServicoException("Tenant é obrigatório.");
            if (string.IsNullOrWhiteSpace(ComNome)) throw new ServicoException("Nome do combo é obrigatório.");
            if (ComPrecoPromocional <= 0) throw new ServicoException("Preço promocional deve ser positivo.");
        }
    }

    public class ComboServico
    {
        public int ComServId { get; private set; }
        public int R_ComId { get; private set; }
        public int R_SerId { get; private set; }

        public Combo Combo { get; private set; }
        public Servico Servico { get; private set; }

        protected ComboServico() { }

        public ComboServico(int rComId, int rSerId)
        {
            R_ComId = rComId;
            R_SerId = rSerId;
        }
    }
}

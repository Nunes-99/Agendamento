using AgendamentoPro.Core.Exceptions;

namespace AgendamentoPro.Core.Entities.Assinaturas
{
    /// <summary>
    /// Catálogo GLOBAL de planos SaaS (não tem R_TenId — é o mesmo cardápio para todos os tenants).
    /// Cada Tenant tem uma Assinatura que referencia 1 Plano.
    /// </summary>
    public class Plano
    {
        public int PlnId { get; private set; }
        public string PlnNome { get; private set; }
        public string PlnDescricao { get; private set; }
        public decimal PlnPreco { get; private set; }
        public int PlnLimiteUnidades { get; private set; }
        public int PlnLimiteProfissionais { get; private set; }
        public int PlnLimiteAgendamentosMes { get; private set; }
        public bool PlnPublico { get; private set; }
        public bool PlnAtivo { get; private set; }
        public int PlnOrdem { get; private set; }
        public DateTime PlnCriadoEm { get; private set; }

        protected Plano() { }

        public Plano(string nome, string descricao, decimal preco,
            int limiteUnidades, int limiteProfissionais, int limiteAgendamentosMes,
            bool publico = true, int ordem = 0)
        {
            PlnNome = nome;
            PlnDescricao = descricao;
            PlnPreco = preco;
            PlnLimiteUnidades = limiteUnidades;
            PlnLimiteProfissionais = limiteProfissionais;
            PlnLimiteAgendamentosMes = limiteAgendamentosMes;
            PlnPublico = publico;
            PlnAtivo = true;
            PlnOrdem = ordem;
            PlnCriadoEm = DateTime.UtcNow;

            Validate();
        }

        public void Atualizar(string nome, string descricao, decimal preco,
            int limiteUnidades, int limiteProfissionais, int limiteAgendamentosMes, int ordem)
        {
            PlnNome = nome;
            PlnDescricao = descricao;
            PlnPreco = preco;
            PlnLimiteUnidades = limiteUnidades;
            PlnLimiteProfissionais = limiteProfissionais;
            PlnLimiteAgendamentosMes = limiteAgendamentosMes;
            PlnOrdem = ordem;
            Validate();
        }

        public void Ativar() => PlnAtivo = true;
        public void Inativar() => PlnAtivo = false;
        public void TornarPublico() => PlnPublico = true;
        public void TornarPrivado() => PlnPublico = false;

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(PlnNome))
                throw new DomainException("Nome do plano é obrigatório.");
            if (PlnPreco <= 0)
                throw new DomainException("Preço do plano deve ser positivo.");
            if (PlnLimiteUnidades == 0)
                throw new DomainException("Limite de unidades deve ser positivo ou -1 para ilimitado.");
            if (PlnLimiteProfissionais == 0)
                throw new DomainException("Limite de profissionais deve ser positivo ou -1 para ilimitado.");
        }

        /// <summary>Retorna true se a quantidade atual respeita o limite (limite -1 = ilimitado).</summary>
        public bool RespeitaLimiteUnidades(int qtdAtual) => PlnLimiteUnidades < 0 || qtdAtual < PlnLimiteUnidades;
        public bool RespeitaLimiteProfissionais(int qtdAtual) => PlnLimiteProfissionais < 0 || qtdAtual < PlnLimiteProfissionais;
        public bool RespeitaLimiteAgendamentos(int qtdMes) => PlnLimiteAgendamentosMes < 0 || qtdMes < PlnLimiteAgendamentosMes;
    }
}

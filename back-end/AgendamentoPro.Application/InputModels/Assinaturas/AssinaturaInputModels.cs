namespace AgendamentoPro.Application.InputModels.Assinaturas
{
    public class CriarAssinaturaInputModel
    {
        public int PlanoId { get; set; }
        /// <summary>E-mail do pagador (geralmente o do admin tenant). MP exige.</summary>
        public string PayerEmail { get; set; }
    }

    public class AlterarPlanoInputModel
    {
        public int NovoPlanoId { get; set; }
    }

    /// <summary>Input model para SuperAdmin gerenciar catálogo de planos.</summary>
    public class PlanoCatalogoInputModel
    {
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal Preco { get; set; }
        public int LimiteUnidades { get; set; }
        public int LimiteProfissionais { get; set; }
        public int LimiteAgendamentosMes { get; set; }
        public bool Publico { get; set; } = true;
        public int Ordem { get; set; }
    }
}

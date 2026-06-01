namespace AgendamentoPro.Application.ViewModels.Assinaturas
{
    public class PlanoViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal Preco { get; set; }
        public int LimiteUnidades { get; set; }
        public int LimiteProfissionais { get; set; }
        public int LimiteAgendamentosMes { get; set; }
    }
}

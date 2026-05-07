namespace AgendamentoPro.Application.ViewModels.Servicos
{
    public class ComboViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string ImagemUrl { get; set; }
        public decimal PrecoOriginal { get; set; }
        public decimal PrecoPromocional { get; set; }
        public decimal Economia => PrecoOriginal - PrecoPromocional;
        public int Ordem { get; set; }
        public bool Ativo { get; set; }
        public List<ComboServicoViewModel> Servicos { get; set; } = new();
    }

    public class ComboServicoViewModel
    {
        public int ServicoId { get; set; }
        public string Nome { get; set; }
        public decimal Preco { get; set; }
        public int DuracaoMinutos { get; set; }
    }
}

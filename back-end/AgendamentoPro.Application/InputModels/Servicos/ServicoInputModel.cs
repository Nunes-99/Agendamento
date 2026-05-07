namespace AgendamentoPro.Application.InputModels.Servicos
{
    public class ServicoInputModel
    {
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public decimal Preco { get; set; }
        public int DuracaoMinutos { get; set; }
        public string ImagemUrl { get; set; }
        public string Categoria { get; set; }
        public int Ordem { get; set; }
        public bool Ativo { get; set; } = true;
    }
}

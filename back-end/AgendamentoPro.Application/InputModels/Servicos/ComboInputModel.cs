namespace AgendamentoPro.Application.InputModels.Servicos
{
    public class ComboInputModel
    {
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string ImagemUrl { get; set; }
        public decimal PrecoPromocional { get; set; }
        public int Ordem { get; set; }
        public bool Ativo { get; set; } = true;
        public List<int> ServicoIds { get; set; } = new();
    }
}

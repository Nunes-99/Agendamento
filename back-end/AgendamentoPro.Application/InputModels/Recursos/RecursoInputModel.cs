namespace AgendamentoPro.Application.InputModels.Recursos
{
    public class RecursoInputModel
    {
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string Tipo { get; set; }
        public string ImagemUrl { get; set; }
        public int Ordem { get; set; }
        public bool Ativo { get; set; } = true;
    }
}

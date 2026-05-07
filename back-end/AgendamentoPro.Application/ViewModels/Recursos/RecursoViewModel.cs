namespace AgendamentoPro.Application.ViewModels.Recursos
{
    public class RecursoViewModel
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string Tipo { get; set; }
        public string ImagemUrl { get; set; }
        public int Ordem { get; set; }
        public bool Ativo { get; set; }
    }
}

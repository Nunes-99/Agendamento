namespace AgendamentoPro.Application.ViewModels.Agendamentos
{
    public class AvaliacaoViewModel
    {
        public int Id { get; set; }
        public int AgendamentoId { get; set; }
        public Guid Token { get; set; }
        public string ClienteNome { get; set; }
        public int? Nota { get; set; }
        public string Comentario { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? RespondidoEm { get; set; }
        public bool Publica { get; set; }
    }

    public class AvaliacaoPublicaViewModel
    {
        public string ClienteNome { get; set; }
        public int Nota { get; set; }
        public string Comentario { get; set; }
        public DateTime RespondidoEm { get; set; }
    }

    public class ResumoAvaliacoesViewModel
    {
        public decimal Media { get; set; }
        public int Total { get; set; }
        public List<AvaliacaoPublicaViewModel> Recentes { get; set; } = new();
    }
}

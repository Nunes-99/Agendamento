namespace AgendamentoPro.Application.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        public int AgendamentosHoje { get; set; }
        public int AgendamentosSemana { get; set; }
        public int AgendamentosMes { get; set; }
        public decimal ReceitaHoje { get; set; }
        public decimal ReceitaMes { get; set; }
        public int PendentesPagamento { get; set; }
        public double TaxaOcupacao { get; set; }
        public List<TopServicoViewModel> TopServicos { get; set; } = new();
        public List<AgendamentoResumoViewModel> ProximosAgendamentos { get; set; } = new();
    }

    public class TopServicoViewModel
    {
        public string Nome { get; set; }
        public int Quantidade { get; set; }
        public decimal ReceitaTotal { get; set; }
    }

    public class AgendamentoResumoViewModel
    {
        public int Id { get; set; }
        public string Cliente { get; set; }
        public string Servico { get; set; }
        public DateTime Data { get; set; }
        public TimeSpan Hora { get; set; }
        public string Status { get; set; }
    }
}

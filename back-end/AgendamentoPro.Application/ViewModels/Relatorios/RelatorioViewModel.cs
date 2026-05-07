namespace AgendamentoPro.Application.ViewModels.Relatorios
{
    public class ReceitaPeriodoViewModel
    {
        public DateTime Data { get; set; }
        public decimal Receita { get; set; }
        public int Quantidade { get; set; }
    }

    public class ServicoMaisVendidoViewModel
    {
        public int ServicoId { get; set; }
        public string Nome { get; set; }
        public int Quantidade { get; set; }
        public decimal ReceitaTotal { get; set; }
    }

    public class TaxaOcupacaoViewModel
    {
        public int RecursoId { get; set; }
        public string RecursoNome { get; set; }
        public int SlotsTotais { get; set; }
        public int SlotsOcupados { get; set; }
        public double Percentual => SlotsTotais == 0 ? 0 : (double)SlotsOcupados / SlotsTotais * 100;
    }

    public class CancelamentoViewModel
    {
        public DateTime Data { get; set; }
        public int Quantidade { get; set; }
        public string MotivoMaisComum { get; set; }
    }
}

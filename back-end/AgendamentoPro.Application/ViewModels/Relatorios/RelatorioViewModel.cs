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

    /// <summary>
    /// Receita acumulada por cliente em agendamentos concluídos.
    /// Útil pra identificar os clientes mais valiosos (top 20 por default).
    /// </summary>
    public class LtvClienteViewModel
    {
        public int ClienteId { get; set; }
        public string Nome { get; set; }
        public string Telefone { get; set; }
        public int QuantidadeAgendamentos { get; set; }
        public decimal ReceitaTotal { get; set; }
        public decimal TicketMedio => QuantidadeAgendamentos == 0 ? 0
            : Math.Round(ReceitaTotal / QuantidadeAgendamentos, 2);
        public DateTime PrimeiroAgendamento { get; set; }
        public DateTime UltimoAgendamento { get; set; }
    }

    /// <summary>
    /// Taxa de no-show por dia da semana ou por hora do dia.
    /// Agendamentos não comparecidos / total efetivado (sem cancelados).
    /// </summary>
    public class NoShowViewModel
    {
        public string Bucket { get; set; } // ex: "Segunda" ou "14h"
        public int NoShow { get; set; }
        public int Concluidos { get; set; }
        public int Total => NoShow + Concluidos;
        public double TaxaPercentual => Total == 0 ? 0
            : Math.Round((double)NoShow / Total * 100, 2);
    }

    /// <summary>Receita + quantidade de agendamentos concluídos por mês.</summary>
    public class SazonalidadeMesViewModel
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public string Rotulo => $"{Ano}-{Mes:D2}";
        public decimal Receita { get; set; }
        public int Quantidade { get; set; }
    }
}

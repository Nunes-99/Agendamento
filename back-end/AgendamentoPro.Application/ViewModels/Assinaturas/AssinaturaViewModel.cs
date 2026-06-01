using AgendamentoPro.Core.Enums;

namespace AgendamentoPro.Application.ViewModels.Assinaturas
{
    public class AssinaturaViewModel
    {
        public int Id { get; set; }
        public int PlanoId { get; set; }
        public string PlanoNome { get; set; }
        public decimal PlanoPreco { get; set; }
        public StatusAssinatura Status { get; set; }
        public string StatusTexto { get; set; }
        public string Gateway { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? TrialAteEm { get; set; }
        public DateTime? ProximoVencimento { get; set; }
        public DateTime? UltimoPagamentoEm { get; set; }
        public DateTime? AtrasoDesde { get; set; }
        public DateTime? ReadOnlyDesde { get; set; }
        public DateTime? CanceladaEm { get; set; }
        public bool PermiteEscrita { get; set; }
        /// <summary>URL pro usuário autorizar o cartão no MP (vazio se já autorizado).</summary>
        public string CheckoutUrl { get; set; }
        public List<FaturaAssinaturaViewModel> Faturas { get; set; } = new();
    }

    public class FaturaAssinaturaViewModel
    {
        public int Id { get; set; }
        public decimal Valor { get; set; }
        public StatusFaturaAssinatura Status { get; set; }
        public string StatusTexto { get; set; }
        public DateTime ReferenciaInicio { get; set; }
        public DateTime ReferenciaFim { get; set; }
        public DateTime VencimentoEm { get; set; }
        public DateTime? PagoEm { get; set; }
    }
}

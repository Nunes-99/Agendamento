namespace AgendamentoPro.Core.Enums
{
    /// <summary>
    /// Estados de uma fatura individual da assinatura SaaS (um ciclo de cobrança).
    /// </summary>
    public enum StatusFaturaAssinatura
    {
        Pendente = 0,
        Paga = 1,
        Recusada = 2,
        Estornada = 3
    }
}

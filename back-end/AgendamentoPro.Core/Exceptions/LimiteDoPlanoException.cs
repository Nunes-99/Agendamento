namespace AgendamentoPro.Core.Exceptions
{
    /// <summary>
    /// Lançada quando uma operação ultrapassaria um limite do plano contratado.
    /// Frontend deve exibir CTA de upgrade.
    /// </summary>
    public class LimiteDoPlanoException : DomainException
    {
        public string Recurso { get; }
        public int LimiteAtual { get; }

        public LimiteDoPlanoException(string recurso, int limiteAtual, string mensagem)
            : base(mensagem)
        {
            Recurso = recurso;
            LimiteAtual = limiteAtual;
        }
    }
}

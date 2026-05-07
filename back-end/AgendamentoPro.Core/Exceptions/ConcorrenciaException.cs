namespace AgendamentoPro.Core.Exceptions
{
    /// <summary>
    /// Lançada quando uma violação de unicidade ou conflito de concorrência ocorre no banco.
    /// Implementada no UnitOfWork da Infrastructure (traduz DbUpdateException).
    /// </summary>
    public class ConcorrenciaException : DomainException
    {
        public ConcorrenciaException(string mensagem) : base(mensagem) { }
    }
}

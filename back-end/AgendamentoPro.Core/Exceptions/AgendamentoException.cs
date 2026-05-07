namespace AgendamentoPro.Core.Exceptions
{
    public class AgendamentoException : DomainException
    {
        public AgendamentoException(string mensagem) : base(mensagem) { }
    }
}

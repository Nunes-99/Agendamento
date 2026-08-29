namespace AgendamentoPro.Core.Exceptions
{
    public class DomainException : Exception
    {
        public DomainException(string mensagem) : base(mensagem) { }
        public DomainException(string mensagem, Exception causa) : base(mensagem, causa) { }
    }
}

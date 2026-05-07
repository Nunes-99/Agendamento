namespace AgendamentoPro.Core.Exceptions
{
    public class ClienteException : DomainException
    {
        public ClienteException(string mensagem) : base(mensagem) { }
    }
}

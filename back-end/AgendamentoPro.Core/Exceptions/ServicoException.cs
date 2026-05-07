namespace AgendamentoPro.Core.Exceptions
{
    public class ServicoException : DomainException
    {
        public ServicoException(string mensagem) : base(mensagem) { }
    }
}

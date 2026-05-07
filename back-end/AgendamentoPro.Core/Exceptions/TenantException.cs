namespace AgendamentoPro.Core.Exceptions
{
    public class TenantException : DomainException
    {
        public TenantException(string mensagem) : base(mensagem) { }
    }
}

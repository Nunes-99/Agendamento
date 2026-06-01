namespace AgendamentoPro.Application.Interfaces.Assinaturas
{
    /// <summary>
    /// Invalida o cache de status de assinatura do tenant. Deve ser chamado depois
    /// de qualquer operação que altere o status (criação, pagamento, cancelamento,
    /// transição de grace period).
    /// </summary>
    public interface IAssinaturaCacheInvalidator
    {
        void Invalidar(int tenantId);
    }
}

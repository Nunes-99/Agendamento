namespace AgendamentoPro.Core.Interfaces.Common
{
    /// <summary>
    /// Resolve o tenant atual a partir do request (subdomínio, header ou claim do JWT).
    /// </summary>
    public interface ITenantContext
    {
        int? TenantId { get; }
        string Slug { get; }
        bool IsResolved { get; }
        void SetTenant(int tenantId, string slug);
    }
}

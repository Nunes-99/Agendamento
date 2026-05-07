using AgendamentoPro.Core.Entities.Usuarios;

namespace AgendamentoPro.Core.Interfaces.Services
{
    public interface ITokenService
    {
        (string Token, DateTime Expiracao, string JwtId) GerarAccessToken(Usuario usuario, string slugTenant);
        string GerarRefreshToken();

        /// <summary>
        /// Token de cliente final (B2C). Validade longa (7 dias) e contém claims clienteId + tenantId.
        /// Usado nas telas /minha-conta após autenticação por OTP via WhatsApp.
        /// </summary>
        (string Token, DateTime Expiracao) GerarTokenCliente(int clienteId, int tenantId, string slugTenant);
    }
}

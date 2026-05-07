using AgendamentoPro.Core.Entities.Usuarios;

namespace AgendamentoPro.Core.Interfaces.Services
{
    public interface ITokenService
    {
        (string Token, DateTime Expiracao, string JwtId) GerarAccessToken(Usuario usuario, string slugTenant);
        string GerarRefreshToken();
    }
}

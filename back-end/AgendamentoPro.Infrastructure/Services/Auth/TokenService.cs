using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AgendamentoPro.Infrastructure.Services.Auth
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config) { _config = config; }

        public (string Token, DateTime Expiracao, string JwtId) GerarAccessToken(Usuario usuario, string slugTenant)
        {
            var settings = _config.GetSection("JwtSettings");
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? settings["SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey não configurado.");
            var horas = int.TryParse(settings["ExpirationInHours"], out var h) ? h : 8;
            var jwtId = Guid.NewGuid().ToString();
            var expiracao = DateTime.UtcNow.AddHours(horas);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.UsuId.ToString()),
                new(ClaimTypes.Email, usuario.UsuEmail),
                new(ClaimTypes.Name, usuario.UsuNome),
                new(ClaimTypes.Role, usuario.UsuPerfil ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, jwtId)
            };
            if (usuario.R_TenId.HasValue)
                claims.Add(new Claim("tenantId", usuario.R_TenId.Value.ToString()));
            if (!string.IsNullOrEmpty(slugTenant))
                claims.Add(new Claim("tenantSlug", slugTenant));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: settings["Issuer"],
                audience: settings["Audience"],
                claims: claims,
                expires: expiracao,
                signingCredentials: creds);
            return (new JwtSecurityTokenHandler().WriteToken(token), expiracao, jwtId);
        }

        public string GerarRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public (string Token, DateTime Expiracao) GerarTokenCliente(int clienteId, int tenantId, string slugTenant)
        {
            var settings = _config.GetSection("JwtSettings");
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? settings["SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey não configurado.");
            var dias = int.TryParse(settings["ClienteTokenDias"], out var d) ? d : 7;
            var expiracao = DateTime.UtcNow.AddDays(dias);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, clienteId.ToString()),
                new(ClaimTypes.Role, "Cliente"),
                new("tipo", "cliente"),
                new("clienteId", clienteId.ToString()),
                new("tenantId", tenantId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            if (!string.IsNullOrEmpty(slugTenant))
                claims.Add(new Claim("tenantSlug", slugTenant));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: settings["Issuer"],
                audience: settings["Audience"],
                claims: claims,
                expires: expiracao,
                signingCredentials: creds);
            return (new JwtSecurityTokenHandler().WriteToken(token), expiracao);
        }
    }
}

using AgendamentoPro.Application.InputModels.Auth;
using AgendamentoPro.Application.Interfaces.Auth;
using AgendamentoPro.Application.ViewModels.Auth;
using AgendamentoPro.Core.Entities.RefreshTokens;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace AgendamentoPro.Application.UseCases.Auth
{
    public class LoginUseCase : ILoginUseCase
    {
        private readonly IUsuarioRepository _usuarios;
        private readonly ITenantRepository _tenants;
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;

        public LoginUseCase(IUsuarioRepository usuarios, ITenantRepository tenants,
            IRefreshTokenRepository refreshTokens, IPasswordHasher hasher,
            ITokenService tokenService, IUnitOfWork uow, IConfiguration config)
        {
            _usuarios = usuarios;
            _tenants = tenants;
            _refreshTokens = refreshTokens;
            _hasher = hasher;
            _tokenService = tokenService;
            _uow = uow;
            _config = config;
        }

        public async Task<LoginViewModel> ExecuteAsync(LoginInputModel input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Senha))
                return null;

            var usuario = await _usuarios.GetByEmailAsync(input.Email.Trim().ToLowerInvariant());
            if (usuario == null || !usuario.UsuAtivo) return null;
            if (!_hasher.Verify(input.Senha, usuario.UsuSenha)) return null;

            string slug = null;
            if (usuario.R_TenId.HasValue)
            {
                var tenant = await _tenants.GetByIdAsync(usuario.R_TenId.Value);
                if (tenant == null || !tenant.TenAtivo) return null;
                slug = tenant.TenSlug;

                if (!string.IsNullOrWhiteSpace(input.TenantSlug) &&
                    !slug.Equals(input.TenantSlug, StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            var (accessToken, expiracao, jwtId) = _tokenService.GerarAccessToken(usuario, slug);
            var refreshTokenStr = _tokenService.GerarRefreshToken();
            var refreshDias = int.TryParse(_config["JwtSettings:RefreshTokenDays"], out var d) ? d : 7;

            var refreshToken = new RefreshToken(usuario.UsuId, refreshTokenStr, jwtId, DateTime.UtcNow.AddDays(refreshDias));
            await _refreshTokens.CreateAsync(refreshToken);

            usuario.RegistrarLogin();
            await _usuarios.UpdateAsync(usuario);
            await _uow.SaveChangesAsync();

            return new LoginViewModel
            {
                UsuId = usuario.UsuId,
                TenantId = usuario.R_TenId,
                TenantSlug = slug,
                Nome = usuario.UsuNome,
                Email = usuario.UsuEmail,
                Perfil = usuario.UsuPerfil,
                AccessToken = accessToken,
                RefreshToken = refreshTokenStr,
                Expiracao = expiracao
            };
        }
    }
}

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
    public class RefreshTokenUseCase : IRefreshTokenUseCase
    {
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IUsuarioRepository _usuarios;
        private readonly ITenantRepository _tenants;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;

        public RefreshTokenUseCase(IRefreshTokenRepository refreshTokens, IUsuarioRepository usuarios,
            ITenantRepository tenants, ITokenService tokenService, IUnitOfWork uow, IConfiguration config)
        {
            _refreshTokens = refreshTokens;
            _usuarios = usuarios;
            _tenants = tenants;
            _tokenService = tokenService;
            _uow = uow;
            _config = config;
        }

        public async Task<LoginViewModel> ExecuteAsync(RefreshTokenInputModel input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.RefreshToken)) return null;

            var token = await _refreshTokens.GetByTokenAsync(input.RefreshToken);
            if (token == null || !token.EstaValido()) return null;

            var usuario = await _usuarios.GetByIdAsync(token.R_UsuId);
            if (usuario == null || !usuario.UsuAtivo) return null;

            string slug = null;
            if (usuario.R_TenId.HasValue)
            {
                var tenant = await _tenants.GetByIdAsync(usuario.R_TenId.Value);
                slug = tenant?.TenSlug;
            }

            token.MarcarUtilizado();
            await _refreshTokens.UpdateAsync(token);

            var (accessToken, expiracao, jwtId) = _tokenService.GerarAccessToken(usuario, slug);
            var novoRefresh = _tokenService.GerarRefreshToken();
            var refreshDias = int.TryParse(_config["JwtSettings:RefreshTokenDays"], out var d) ? d : 7;
            var novo = new RefreshToken(usuario.UsuId, novoRefresh, jwtId, DateTime.UtcNow.AddDays(refreshDias));
            await _refreshTokens.CreateAsync(novo);

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
                RefreshToken = novoRefresh,
                Expiracao = expiracao
            };
        }
    }
}

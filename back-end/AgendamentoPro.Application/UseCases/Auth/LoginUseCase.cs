using AgendamentoPro.Application.InputModels.Auth;
using AgendamentoPro.Application.Interfaces.Auth;
using AgendamentoPro.Application.ViewModels.Auth;
using AgendamentoPro.Core.Entities.RefreshTokens;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Application.UseCases.Auth
{
    public class LoginUseCase : ILoginUseCase
    {
        // Lockout: 5 falhas consecutivas → bloqueia 15 min. Reset em login OK ou troca de senha.
        private const int TentativasMax = 5;
        private static readonly TimeSpan DuracaoBloqueio = TimeSpan.FromMinutes(15);

        private readonly IUsuarioRepository _usuarios;
        private readonly ITenantRepository _tenants;
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokenService;
        private readonly ITotpService _totp;
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;
        private readonly ILogger<LoginUseCase> _logger;

        public LoginUseCase(IUsuarioRepository usuarios, ITenantRepository tenants,
            IRefreshTokenRepository refreshTokens, IPasswordHasher hasher,
            ITokenService tokenService, ITotpService totp,
            IUnitOfWork uow, IConfiguration config, ILogger<LoginUseCase> logger)
        {
            _usuarios = usuarios;
            _tenants = tenants;
            _refreshTokens = refreshTokens;
            _hasher = hasher;
            _tokenService = tokenService;
            _totp = totp;
            _uow = uow;
            _config = config;
            _logger = logger;
        }

        public async Task<LoginViewModel> ExecuteAsync(LoginInputModel input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Senha))
                return null;

            var usuario = await _usuarios.GetByEmailAsync(input.Email.Trim().ToLowerInvariant());
            if (usuario == null || !usuario.UsuAtivo) return null;

            // Lockout: bloqueado por excesso de tentativas
            if (usuario.EstaBloqueado(DateTime.UtcNow))
            {
                _logger.LogWarning("Login negado: usuário {Email} bloqueado até {Ate}.",
                    usuario.UsuEmail, usuario.UsuBloqueadoAte);
                return new LoginViewModel
                {
                    Mensagem = "Conta bloqueada por excesso de tentativas. Tente novamente em alguns minutos."
                };
            }

            if (!_hasher.Verify(input.Senha, usuario.UsuSenha))
            {
                usuario.RegistrarFalhaLogin(TentativasMax, DuracaoBloqueio);
                await _usuarios.UpdateAsync(usuario);
                await _uow.SaveChangesAsync();
                return null;
            }

            // 2FA TOTP: se ativo, exige código antes de emitir tokens.
            if (usuario.UsuTotpAtivo && !string.IsNullOrEmpty(usuario.UsuTotpSecret))
            {
                if (string.IsNullOrEmpty(input.CodigoTotp))
                    return new LoginViewModel { RequerTotp = true };
                if (!_totp.Verificar(usuario.UsuTotpSecret, input.CodigoTotp, DateTime.UtcNow))
                {
                    usuario.RegistrarFalhaLogin(TentativasMax, DuracaoBloqueio);
                    await _usuarios.UpdateAsync(usuario);
                    await _uow.SaveChangesAsync();
                    return null;
                }
            }

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

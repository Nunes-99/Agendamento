using AgendamentoPro.Application.InputModels.Auth;
using AgendamentoPro.Application.Interfaces.Auth;
using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;

namespace AgendamentoPro.Application.UseCases.Auth
{
    public class SolicitarResetSenhaUseCase : ISolicitarResetSenhaUseCase
    {
        private static readonly TimeSpan Validade = TimeSpan.FromHours(1);

        private readonly IUsuarioRepository _usuarios;
        private readonly IPasswordResetRepository _resets;
        private readonly IEmailSender _email;
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;
        private readonly ILogger<SolicitarResetSenhaUseCase> _logger;

        public SolicitarResetSenhaUseCase(IUsuarioRepository usuarios, IPasswordResetRepository resets,
            IEmailSender email, IUnitOfWork uow, IConfiguration config,
            ILogger<SolicitarResetSenhaUseCase> logger)
        {
            _usuarios = usuarios; _resets = resets; _email = email;
            _uow = uow; _config = config; _logger = logger;
        }

        public async Task<SolicitarResetSenhaResultViewModel> ExecuteAsync(SolicitarResetSenhaInputModel input)
        {
            var emailLower = (input.Email ?? string.Empty).Trim().ToLowerInvariant();
            var usuario = await _usuarios.GetByEmailAsync(emailLower);

            // Política: NÃO revela ao requester se o email existe (defesa contra enumeração).
            // Sempre retorna 200, mas só gera token quando há usuário ativo.
            if (usuario == null || !usuario.UsuAtivo)
            {
                _logger.LogInformation("Reset solicitado para e-mail não cadastrado/inativo: {Email}", emailLower);
                return new SolicitarResetSenhaResultViewModel { LinkGerado = false };
            }

            // Invalida tokens anteriores do mesmo usuário
            await _resets.InvalidarPendentesAsync(usuario.UsuId);

            var token = GerarToken();
            var reset = new PasswordReset(usuario.UsuId, token, Validade);
            await _resets.CreateAsync(reset);
            await _uow.SaveChangesAsync();

            var publicUrl = (Environment.GetEnvironmentVariable("APP_PUBLIC_URL")
                ?? _config["App:PublicUrl"] ?? "http://localhost:4200").TrimEnd('/');
            // Aponta para o frontend, que tem rota /redefinir-senha?token=...
            // Permite ajustar via APP_FRONTEND_URL se diferente.
            var frontUrl = (Environment.GetEnvironmentVariable("APP_FRONTEND_URL")
                ?? _config["App:FrontendUrl"] ?? publicUrl).TrimEnd('/');
            var link = $"{frontUrl}/redefinir-senha?token={Uri.EscapeDataString(token)}";

            // SEGURANÇA: NUNCA logar o link completo em produção — qualquer ops
            // com acesso ao agregador de logs ganharia bypass de senha. Em dev
            // ainda logamos pra facilitar testes manuais (sem SMTP configurado).
            var isProd = string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Production", StringComparison.OrdinalIgnoreCase);
            if (isProd)
            {
                _logger.LogInformation(
                    "Reset de senha gerado para {Email} (válido por {Validade}h). Link enviado por email se SMTP ativo.",
                    usuario.UsuEmail, Validade.TotalHours);
            }
            else
            {
                _logger.LogWarning("==== LINK DE RESET DE SENHA (dev) ===="
                    + "\nUsuário: {Email}"
                    + "\nLink (válido por {Validade}h): {Link}"
                    + "\n================================",
                    usuario.UsuEmail, Validade.TotalHours, link);
            }

            // Tenta enviar por e-mail. Se SMTP não estiver configurado, apenas loga
            // (operador entrega o link manualmente).
            if (_email.Ativo)
            {
                var html = MontarEmailHtml(usuario.UsuNome, link, Validade);
                var texto = MontarEmailTexto(usuario.UsuNome, link, Validade);
                await _email.EnviarAsync(usuario.UsuEmail, "Redefinir senha — AgendamentoPro", html, texto);
            }

            return new SolicitarResetSenhaResultViewModel
            {
                LinkGerado = true,
                LinkReset = link,
                ExpiraEm = reset.RpsExpiraEm
            };
        }

        private static string MontarEmailHtml(string nome, string link, TimeSpan validade) =>
            $@"<div style='font-family:sans-serif;max-width:480px;margin:auto;padding:1.5rem'>
                <h2 style='color:#1976d2;margin-top:0'>Redefinir senha</h2>
                <p>Olá, {WebUtility.HtmlEncode(nome)}!</p>
                <p>Recebemos uma solicitação para redefinir a senha da sua conta no AgendamentoPro.</p>
                <p style='text-align:center;margin:1.5rem 0'>
                    <a href='{link}' style='background:#1976d2;color:#fff;padding:0.75rem 1.5rem;border-radius:0.5rem;text-decoration:none;display:inline-block'>
                        Redefinir minha senha
                    </a>
                </p>
                <p style='color:#666;font-size:0.85rem'>Este link é válido por {validade.TotalHours:0} hora(s) e só pode ser usado uma vez.</p>
                <p style='color:#666;font-size:0.85rem'>Se você não solicitou, ignore este e-mail. Sua senha continua segura.</p>
                <p style='color:#999;font-size:0.75rem;margin-top:2rem'>Se o botão não funcionar, copie e cole esta URL no navegador:<br>{link}</p>
            </div>";

        private static string MontarEmailTexto(string nome, string link, TimeSpan validade) =>
            $"Olá, {nome}!\n\n" +
            $"Recebemos uma solicitação para redefinir a senha da sua conta no AgendamentoPro.\n\n" +
            $"Acesse o link abaixo para escolher uma nova senha (válido por {validade.TotalHours:0} hora):\n{link}\n\n" +
            "Se você não solicitou, ignore este e-mail.\n";

        private static string GerarToken()
        {
            // 32 bytes => 256 bits, base64url (URL-safe)
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }

    public class RedefinirSenhaUseCase : IRedefinirSenhaUseCase
    {
        private readonly IPasswordResetRepository _resets;
        private readonly IUsuarioRepository _usuarios;
        private readonly IPasswordHasher _hasher;
        private readonly IRefreshTokenRepository _refresh;
        private readonly IUnitOfWork _uow;

        public RedefinirSenhaUseCase(IPasswordResetRepository resets, IUsuarioRepository usuarios,
            IPasswordHasher hasher, IRefreshTokenRepository refresh, IUnitOfWork uow)
        {
            _resets = resets; _usuarios = usuarios; _hasher = hasher;
            _refresh = refresh; _uow = uow;
        }

        public async Task ExecuteAsync(RedefinirSenhaInputModel input)
        {
            var reset = await _resets.GetByTokenAsync(input.Token)
                ?? throw new UsuarioException("Token inválido ou expirado.");

            if (!reset.EstaValido(DateTime.UtcNow))
                throw new UsuarioException("Token inválido ou expirado.");

            var usuario = await _usuarios.GetByIdAsync(reset.R_UsuId)
                ?? throw new UsuarioException("Usuário não encontrado.");

            usuario.AlterarSenha(_hasher.Hash(input.NovaSenha));
            await _usuarios.UpdateAsync(usuario);

            reset.MarcarUsado();
            await _resets.UpdateAsync(reset);

            // Revoga refresh tokens existentes - força novo login com a senha nova
            await _refresh.RevogarTodosDoUsuarioAsync(usuario.UsuId);

            await _uow.SaveChangesAsync();
        }
    }
}

using AgendamentoPro.Application.InputModels.Auth;
using AgendamentoPro.Application.Interfaces.Auth;
using AgendamentoPro.Application.ViewModels.Auth;
using AgendamentoPro.Core.Common;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Usuarios;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace AgendamentoPro.Application.UseCases.Auth
{
    /// <summary>
    /// Autenticação de cliente final via OTP por WhatsApp.
    /// Solicitar: gera código de 6 dígitos, salva hash, envia via WhatsApp template.
    /// Validar: confere código, registra falhas, retorna JWT cliente em caso de sucesso.
    ///
    /// Limites: 1 código por minuto, 5 códigos por hora por telefone, 3 tentativas por código,
    /// validade 10 minutos.
    /// </summary>
    public class OtpUseCase : IOtpUseCase
    {
        private static readonly TimeSpan ValidadeOtp = TimeSpan.FromMinutes(10);
        private const int CooldownSegundos = 60;
        private const int LimiteHora = 5;

        private readonly IOtpChallengeRepository _otps;
        private readonly IClienteRepository _clientes;
        private readonly INotificadorWhatsApp _whats;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _hasher;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<OtpUseCase> _logger;

        // Aspnetcore environment lido via env var pra evitar dependência de Microsoft.Extensions.Hosting
        // (Application não deve referenciar runtime do AspNetCore).
        private static readonly bool IsDev = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development", StringComparison.OrdinalIgnoreCase);

        public OtpUseCase(IOtpChallengeRepository otps, IClienteRepository clientes,
            INotificadorWhatsApp whats, ITokenService tokenService, IPasswordHasher hasher,
            IUnitOfWork uow, ILogger<OtpUseCase> logger)
        {
            _otps = otps;
            _clientes = clientes;
            _whats = whats;
            _tokenService = tokenService;
            _hasher = hasher;
            _uow = uow;
            _logger = logger;
        }

        public async Task<SolicitarOtpResultViewModel> SolicitarAsync(int tenantId, string slugTenant, SolicitarOtpInputModel input)
        {
            var telefone = NormalizarTelefone(input?.Telefone);
            if (string.IsNullOrEmpty(telefone) || telefone.Length < 10)
                return new SolicitarOtpResultViewModel { Enviado = false };

            // Throttle por hora
            var recentes = await _otps.ContarRecentesAsync(tenantId, telefone, DateTime.UtcNow.AddHours(-1));
            if (recentes >= LimiteHora)
            {
                _logger.LogWarning("OTP throttled (hora): tenant {Tid} telefone {Tel}", tenantId, PiiMask.Telefone(telefone));
                return new SolicitarOtpResultViewModel { Enviado = false };
            }

            // Cooldown: bloqueia novo envio se último foi há menos de 60s
            var ultimo = await _otps.GetUltimoAtivoAsync(tenantId, telefone);
            if (ultimo != null && ultimo.OtpCriadoEm > DateTime.UtcNow.AddSeconds(-CooldownSegundos))
            {
                var faltam = CooldownSegundos - (int)(DateTime.UtcNow - ultimo.OtpCriadoEm).TotalSeconds;
                return new SolicitarOtpResultViewModel
                {
                    Enviado = false,
                    CooldownSegundos = Math.Max(faltam, 1),
                    ExpiraEm = ultimo.OtpExpiraEm
                };
            }

            var codigo = GerarCodigo6Digitos();
            var hash = _hasher.Hash(codigo);
            var challenge = new OtpChallenge(tenantId, telefone, hash, ValidadeOtp);
            await _otps.CreateAsync(challenge);
            await _uow.SaveChangesAsync();

            // Envia via WhatsApp (template pra fora da janela de 24h, sem template em dev/no-op)
            try
            {
                if (_whats.Ativo)
                {
                    await _whats.EnviarTemplateAsync(telefone, "otp_codigo_verificacao", "pt_BR", codigo);
                }
                else if (IsDev)
                {
                    // SEGURANÇA: só loga o código em Development. Em produção, se WhatsApp
                    // estiver inativo, qualquer ops com acesso aos logs viria a ter
                    // bypass de auth — logar = login sem credenciais.
                    _logger.LogInformation("WhatsApp inativo — código OTP {Codigo} para {Tel} (modo dev)",
                        codigo, telefone);
                }
                else
                {
                    _logger.LogWarning("WhatsApp inativo em produção: OTP gerado mas não enviado para {Tel}. " +
                        "Configure WHATSAPP_ACCESS_TOKEN — cliente não receberá o código.",
                        PiiMask.Telefone(telefone));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao enviar OTP para {Tel}", PiiMask.Telefone(telefone));
            }

            return new SolicitarOtpResultViewModel
            {
                Enviado = true,
                ExpiraEm = challenge.OtpExpiraEm,
                CooldownSegundos = CooldownSegundos,
                CodigoDev = IsDev ? codigo : null
            };
        }

        public async Task<ValidarOtpResultViewModel> ValidarAsync(int tenantId, string slugTenant, ValidarOtpInputModel input)
        {
            var telefone = NormalizarTelefone(input?.Telefone);
            var codigo = (input?.Codigo ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(telefone) || codigo.Length != 6)
                return new ValidarOtpResultViewModel { Valido = false, Mensagem = "Dados inválidos." };

            var challenge = await _otps.GetUltimoAtivoAsync(tenantId, telefone);
            if (challenge == null || !challenge.Disponivel(DateTime.UtcNow))
                return new ValidarOtpResultViewModel { Valido = false, Mensagem = "Código expirado ou não solicitado." };

            if (!_hasher.Verify(codigo, challenge.OtpCodigoHash))
            {
                challenge.RegistrarFalha();
                await _otps.UpdateAsync(challenge);
                await _uow.SaveChangesAsync();
                return new ValidarOtpResultViewModel { Valido = false, Mensagem = "Código incorreto." };
            }

            challenge.MarcarUsado();
            await _otps.UpdateAsync(challenge);

            // Localiza cliente. Se não existir, cria com nome temporário (depois pode editar).
            var cliente = await _clientes.GetByTelefoneAsync(tenantId, telefone);
            if (cliente == null)
            {
                cliente = new Cliente(tenantId, "Cliente", null, telefone, telefone, null);
                await _clientes.CreateAsync(cliente);
            }

            await _uow.SaveChangesAsync();

            var (token, expiracao) = _tokenService.GerarTokenCliente(cliente.CliId, tenantId, slugTenant);
            return new ValidarOtpResultViewModel
            {
                Valido = true,
                Token = token,
                Expiracao = expiracao,
                ClienteId = cliente.CliId,
                ClienteNome = cliente.CliNome
            };
        }

        private static string NormalizarTelefone(string telefone)
        {
            if (string.IsNullOrEmpty(telefone)) return null;
            var sb = new StringBuilder();
            foreach (var c in telefone) if (char.IsDigit(c)) sb.Append(c);
            return sb.ToString();
        }

        private static string GerarCodigo6Digitos()
        {
            // Cryptographically secure: usa RandomNumberGenerator pra evitar previsibilidade
            Span<byte> bytes = stackalloc byte[4];
            RandomNumberGenerator.Fill(bytes);
            var v = BitConverter.ToUInt32(bytes) % 1_000_000;
            return v.ToString("D6");
        }
    }
}

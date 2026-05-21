using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Setup e desativação de 2FA (TOTP) do usuário autenticado.
    /// Fluxo:
    ///   1. POST /iniciar  → gera secret, retorna QR otpauth:// para escanear
    ///   2. POST /confirmar → usuário cola código de 6 dígitos do app autenticador,
    ///                        backend valida e marca UsuTotpAtivo=true
    ///   3. POST /desativar → exige código atual e desativa
    /// </summary>
    [ApiController]
    [Route("api/v1/admin/2fa")]
    [Authorize]
    [Produces("application/json")]
    public class TwoFactorController : BaseTenantController
    {
        [HttpPost("iniciar")]
        public async Task<IActionResult> Iniciar(
            [FromServices] IUsuarioRepository usuarios,
            [FromServices] ITotpService totp,
            [FromServices] IUnitOfWork uow)
        {
            var usuId = ObterUsuarioId();
            if (usuId == 0) return Unauthorized();
            var u = await usuarios.GetByIdAsync(usuId);
            if (u == null) return NotFound();

            // Se já está ATIVO, bloquear: sobrescrever o secret aqui permitiria
            // que um atacante com sessão bloqueasse o usuário (ele perderia acesso
            // ao gerador antigo, e o novo secret estaria com o atacante).
            // Para reconfigurar, exigir Desativar (que exige código atual) primeiro.
            if (u.UsuTotpAtivo)
                return BadRequest(new
                {
                    message = "2FA já está ativo. Para reconfigurar, desative primeiro usando o código atual."
                });

            var secret = totp.GerarSecret();
            // DefinirTotpSecret salva o secret mas NÃO ativa 2FA — fica pendente
            // de confirmação via /confirmar.
            u.DefinirTotpSecret(secret);

            await usuarios.UpdateAsync(u);
            await uow.SaveChangesAsync();

            var url = totp.GerarOtpAuthUrl(secret, u.UsuEmail, "AgendamentoPro");
            return Ok(new { secret, otpauthUrl = url, ativo = u.UsuTotpAtivo });
        }

        [HttpPost("confirmar")]
        public async Task<IActionResult> Confirmar(
            [FromServices] IUsuarioRepository usuarios,
            [FromServices] ITotpService totp,
            [FromServices] IUnitOfWork uow,
            [FromQuery] string codigo)
        {
            var usuId = ObterUsuarioId();
            if (usuId == 0) return Unauthorized();
            var u = await usuarios.GetByIdAsync(usuId);
            if (u == null || string.IsNullOrEmpty(u.UsuTotpSecret))
                return BadRequest(new { message = "Inicie o setup de 2FA primeiro." });

            var step = totp.VerificarERetornarStep(u.UsuTotpSecret, codigo, DateTime.UtcNow);
            if (step < 0 || !u.RegistrarTotpStep(step))
                return BadRequest(new { message = "Código inválido." });

            // Confirmação OK: agora ativa 2FA de fato.
            u.AtivarTotp();
            await usuarios.UpdateAsync(u);
            await uow.SaveChangesAsync();
            return Ok(new { ativo = true });
        }

        [HttpPost("desativar")]
        public async Task<IActionResult> Desativar(
            [FromServices] IUsuarioRepository usuarios,
            [FromServices] ITotpService totp,
            [FromServices] IUnitOfWork uow,
            [FromQuery] string codigo)
        {
            var usuId = ObterUsuarioId();
            if (usuId == 0) return Unauthorized();
            var u = await usuarios.GetByIdAsync(usuId);
            if (u == null) return NotFound();
            if (!u.UsuTotpAtivo) return Ok(new { ativo = false });

            // Exige código atual antes de desativar (proteção contra session hijack)
            // Também rejeita replay (mesmo código já usado).
            var step = totp.VerificarERetornarStep(u.UsuTotpSecret, codigo, DateTime.UtcNow);
            if (step < 0 || !u.RegistrarTotpStep(step))
                return BadRequest(new { message = "Código inválido." });

            u.DesativarTotp();
            await usuarios.UpdateAsync(u);
            await uow.SaveChangesAsync();
            return Ok(new { ativo = false });
        }

        private int ObterUsuarioId()
        {
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            return int.TryParse(idStr, out var id) ? id : 0;
        }
    }
}

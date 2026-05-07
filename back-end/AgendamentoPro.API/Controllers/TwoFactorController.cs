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
    [Route("api/admin/2fa")]
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

            var secret = totp.GerarSecret();
            // DefinirTotpSecret salva o secret mas NÃO ativa 2FA — fica pendente
            // de confirmação via /confirmar. Se o usuário já tinha 2FA ativo,
            // a chamada não troca o secret antigo até confirmar (segurança).
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

            if (!totp.Verificar(u.UsuTotpSecret, codigo, DateTime.UtcNow))
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
            if (!totp.Verificar(u.UsuTotpSecret, codigo, DateTime.UtcNow))
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

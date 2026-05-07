using AgendamentoPro.Application.InputModels.Auth;
using AgendamentoPro.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AgendamentoPro.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromServices] ILoginUseCase useCase, [FromBody] LoginInputModel input)
        {
            var result = await useCase.ExecuteAsync(input);
            if (result == null) return Unauthorized(new { message = "Credenciais inválidas." });
            return Ok(result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromServices] IRefreshTokenUseCase useCase, [FromBody] RefreshTokenInputModel input)
        {
            var result = await useCase.ExecuteAsync(input);
            if (result == null) return Unauthorized(new { message = "Refresh token inválido." });
            return Ok(result);
        }
    }
}

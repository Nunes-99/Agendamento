using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Recebe erros acontecidos no NAVEGADOR e os grava no log do servidor.
    ///
    /// Existe por uma lição concreta: o SignalR ficou sem conectar por causa de
    /// CORS e de token, e isso só aparecia no console do navegador de quem
    /// estivesse com o F12 aberto. Nada no log do servidor, nenhuma reclamação
    /// possível de investigar — o recurso simplesmente não funcionava, em
    /// silêncio, para todo mundo.
    ///
    /// Não substitui um Sentry da vida; substitui o nada, sem depender de conta
    /// em serviço externo. Os eventos entram no mesmo Serilog do resto, já
    /// enriquecidos com CorrelationId/TenantId/UserId pelo middleware.
    /// </summary>
    [ApiController]
    [Route("api/v1/erros-cliente")]
    public class ErrosClienteController : ControllerBase
    {
        /// <summary>
        /// Anônimo de propósito: erro em tela pública (agendamento, pagamento) é
        /// justamente o que mais interessa, e ali não há usuário logado.
        /// Limitado pela política de webhook para não virar canal de spam.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("webhook")]
        public IActionResult Registrar(
            [FromBody] ErroClienteInput input,
            [FromServices] ILogger<ErrosClienteController> logger)
        {
            if (string.IsNullOrWhiteSpace(input?.Mensagem))
                return BadRequest();

            // Warning, e não Error: é erro do lado do cliente e não deve disparar
            // alarme de indisponibilidade do servidor. Corta o que for absurdo em
            // tamanho — a origem não é confiável.
            logger.LogWarning(
                "Erro no navegador: {Mensagem} | tela={Rota} | status={Status} | url={UrlChamada} | agente={Agente} | pilha={Pilha}",
                Cortar(input.Mensagem, 500),
                Cortar(input.Rota, 200),
                input.Status,
                Cortar(input.UrlChamada, 300),
                Cortar(Request.Headers.UserAgent.ToString(), 200),
                Cortar(input.Pilha, 2000));

            // 204: o navegador não tem nada a fazer com a resposta, e um corpo aqui
            // só serviria para o relato de erro gerar outro erro.
            return NoContent();
        }

        private static string Cortar(string valor, int max) =>
            string.IsNullOrEmpty(valor) ? "" : valor.Length <= max ? valor : valor[..max] + "…";

        public class ErroClienteInput
        {
            public string Mensagem { get; set; }
            public string Pilha { get; set; }
            /// <summary>Rota do Angular em que o usuário estava.</summary>
            public string Rota { get; set; }
            /// <summary>Quando vem de uma chamada HTTP: status e endereço.</summary>
            public int Status { get; set; }
            public string UrlChamada { get; set; }
        }
    }
}

using System.Text.Json;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendamentoPro.API.Controllers
{
    /// <summary>
    /// Vitrine do tenant: anúncios/promoções que o lojista gerencia e o cliente
    /// final vê na home pública. Persistidos como JSON em ConfiguracaoTenant
    /// (chave "vitrine.anuncios") — sem migration, dentro do limite de 4000 chars
    /// da coluna (por isso os tetos de quantidade e tamanho abaixo).
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class VitrineController : BaseTenantController
    {
        private const string ChaveAnuncios = "vitrine.anuncios";
        private const int MaxAnuncios = 8;
        private const int MaxTitulo = 60;
        private const int MaxTexto = 200;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public class AnuncioInput
        {
            public string Titulo { get; set; }
            public string Texto { get; set; }
            /// <summary>Destaque usa a cor de acento do tenant na vitrine.</summary>
            public bool Destaque { get; set; }
            public bool Ativo { get; set; } = true;
        }

        [HttpGet("api/v1/admin/vitrine/anuncios")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> ListarAdmin(
            [FromServices] IConfiguracaoTenantRepository configs,
            [FromServices] ITenantContext ctx)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await LerAnuncios(configs, tid));
        }

        [HttpPut("api/v1/admin/vitrine/anuncios")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> Salvar(
            [FromServices] IConfiguracaoTenantRepository configs,
            [FromServices] IUnitOfWork uow,
            [FromServices] ITenantContext ctx,
            [FromBody] List<AnuncioInput> anuncios)
        {
            var tid = RequireTenantId(ctx);
            anuncios ??= new List<AnuncioInput>();

            if (anuncios.Count > MaxAnuncios)
                return BadRequest(new { message = $"Máximo de {MaxAnuncios} anúncios." });
            foreach (var a in anuncios)
            {
                if (string.IsNullOrWhiteSpace(a.Titulo))
                    return BadRequest(new { message = "Todo anúncio precisa de um título." });
                if (a.Titulo.Length > MaxTitulo)
                    return BadRequest(new { message = $"Título com no máximo {MaxTitulo} caracteres." });
                if ((a.Texto?.Length ?? 0) > MaxTexto)
                    return BadRequest(new { message = $"Texto com no máximo {MaxTexto} caracteres." });
                a.Titulo = a.Titulo.Trim();
                a.Texto = a.Texto?.Trim();
            }

            var json = JsonSerializer.Serialize(anuncios, JsonOpts);
            var existente = await configs.GetByChaveAsync(tid, ChaveAnuncios);
            if (existente == null)
            {
                await configs.CreateAsync(new ConfiguracaoTenant(tid, ChaveAnuncios, json, "vitrine", sensivel: false));
            }
            else
            {
                existente.AlterarValor(json);
                await configs.UpdateAsync(existente);
                await uow.SaveChangesAsync();
            }

            return Ok(anuncios);
        }

        /// <summary>Área pública: só os anúncios ativos, na ordem em que o lojista os deixou.</summary>
        [HttpGet("api/v1/t/{slug}/anuncios")]
        [AllowAnonymous]
        public async Task<IActionResult> ListarPublico(
            [FromServices] IConfiguracaoTenantRepository configs,
            [FromServices] ITenantContext ctx,
            string slug)
        {
            var tid = RequireTenantId(ctx);
            var todos = await LerAnuncios(configs, tid);
            return Ok(todos.Where(a => a.Ativo));
        }

        private static async Task<List<AnuncioInput>> LerAnuncios(IConfiguracaoTenantRepository configs, int tenantId)
        {
            var cfg = await configs.GetByChaveAsync(tenantId, ChaveAnuncios);
            if (cfg == null || string.IsNullOrWhiteSpace(cfg.CfgValor)) return new List<AnuncioInput>();
            try
            {
                return JsonSerializer.Deserialize<List<AnuncioInput>>(cfg.CfgValor, JsonOpts) ?? new List<AnuncioInput>();
            }
            catch (JsonException)
            {
                // Valor corrompido não pode derrubar a home pública do tenant.
                return new List<AnuncioInput>();
            }
        }
    }
}

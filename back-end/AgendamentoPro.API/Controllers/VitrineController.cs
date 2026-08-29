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

        private const string ChaveGaleria = "vitrine.galeria";
        private const int MaxFotosGaleria = 12;
        private const int MaxLegenda = 100;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public class AnuncioInput
        {
            public string Titulo { get; set; }
            public string Texto { get; set; }
            /// <summary>Destaque usa a cor de acento do tenant na vitrine.</summary>
            public bool Destaque { get; set; }
            public bool Ativo { get; set; } = true;
        }

        public class FotoGaleriaInput
        {
            public string Url { get; set; }
            public string Legenda { get; set; }
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

        /// <summary>
        /// Upload de imagem da vitrine (logo, banner ou favicon). Salva no storage de
        /// fotos e JÁ APLICA na personalização do tenant — upload = publicado, sem
        /// depender de um "salvar" separado. A imagem anterior, se era um upload da
        /// vitrine, é removida (best-effort).
        /// </summary>
        [HttpPost("api/v1/admin/vitrine/imagem")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> UploadImagem(
            [FromServices] Core.Interfaces.Services.IFotoStorage storage,
            [FromServices] Core.Interfaces.Services.IVitrineImagemProcessor processador,
            [FromServices] ITenantRepository tenants,
            [FromServices] IUnitOfWork uow,
            [FromServices] ITenantContext ctx,
            [FromQuery] string tipo,
            IFormFile arquivo)
        {
            var tid = RequireTenantId(ctx);

            tipo = (tipo ?? string.Empty).Trim().ToLowerInvariant();
            if (tipo is not ("logo" or "banner" or "favicon"))
                return BadRequest(new { message = "Tipo deve ser logo, banner ou favicon." });
            if (arquivo == null || arquivo.Length == 0)
                return BadRequest(new { message = "Envie um arquivo de imagem (jpg, png, webp ou gif)." });

            var tenant = await tenants.GetByIdAsync(tid);
            if (tenant == null) return NotFound();

            string urlAntiga = tipo switch
            {
                "logo" => tenant.TenLogoUrl,
                "banner" => tenant.TenBannerUrl,
                _ => tenant.TenFaviconUrl
            };

            Core.Interfaces.Services.FotoSalvaResult salvo;
            try
            {
                // Crop/resize por tipo ANTES do storage: logo cabe em 512², banner é
                // cortado para capa 3:1 e favicon vira PNG 128². Também valida que o
                // conteúdo decodifica como imagem — extensão certa com bytes errados
                // não passa. O formato/extensão finais vêm do processador (favicon
                // sempre sai .png, independente do que o lojista mandou).
                await using var stream = arquivo.OpenReadStream();
                var processada = await processador.ProcessarAsync(tipo, stream);
                await using var conteudo = processada.Conteudo;
                var nomeFinal = Path.ChangeExtension(
                    string.IsNullOrWhiteSpace(arquivo.FileName) ? tipo : arquivo.FileName,
                    processada.Extensao);
                salvo = await storage.SalvarVitrineAsync(tid, tipo, nomeFinal, processada.ContentType, conteudo);
            }
            catch (InvalidOperationException ex)
            {
                // Imagem inválida/corrompida, tipo não permitido ou arquivo grande demais.
                return BadRequest(new { message = ex.Message });
            }

            tenant.AtualizarPersonalizacao(
                tipo == "logo" ? salvo.Url : tenant.TenLogoUrl,
                tipo == "banner" ? salvo.Url : tenant.TenBannerUrl,
                tipo == "favicon" ? salvo.Url : tenant.TenFaviconUrl,
                tenant.TenCorPrimaria, tenant.TenCorSecundaria, tenant.TenCorAcento, tenant.TenFonte);
            await tenants.UpdateAsync(tenant);
            await uow.SaveChangesAsync();

            // Só apaga a antiga depois do novo estado persistido, e só se ela era um
            // upload da vitrine DESTE tenant (URL externa do lojista fica intocada).
            if (!string.IsNullOrWhiteSpace(urlAntiga)
                && urlAntiga.Contains($"/{tid}/vitrine/", StringComparison.OrdinalIgnoreCase))
            {
                await storage.RemoverAsync(urlAntiga);
            }

            return Ok(new { url = salvo.Url });
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

        // ===== Galeria de fotos do estabelecimento =====

        [HttpGet("api/v1/admin/vitrine/galeria")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> ListarGaleriaAdmin(
            [FromServices] IConfiguracaoTenantRepository configs,
            [FromServices] ITenantContext ctx)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await LerGaleria(configs, tid));
        }

        /// <summary>Adiciona uma foto: processa (cabe em 1600², sem crop), salva e anexa à lista.</summary>
        [HttpPost("api/v1/admin/vitrine/galeria")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> AdicionarFotoGaleria(
            [FromServices] Core.Interfaces.Services.IFotoStorage storage,
            [FromServices] Core.Interfaces.Services.IVitrineImagemProcessor processador,
            [FromServices] IConfiguracaoTenantRepository configs,
            [FromServices] IUnitOfWork uow,
            [FromServices] ITenantContext ctx,
            IFormFile arquivo)
        {
            var tid = RequireTenantId(ctx);
            if (arquivo == null || arquivo.Length == 0)
                return BadRequest(new { message = "Envie um arquivo de imagem (jpg, png, webp ou gif)." });

            var fotos = await LerGaleria(configs, tid);
            if (fotos.Count >= MaxFotosGaleria)
                return BadRequest(new { message = $"A galeria comporta no máximo {MaxFotosGaleria} fotos." });

            try
            {
                await using var stream = arquivo.OpenReadStream();
                var processada = await processador.ProcessarAsync("galeria", stream);
                await using var conteudo = processada.Conteudo;
                var nomeFinal = Path.ChangeExtension(
                    string.IsNullOrWhiteSpace(arquivo.FileName) ? "galeria" : arquivo.FileName,
                    processada.Extensao);
                var salvo = await storage.SalvarVitrineAsync(tid, "galeria", nomeFinal, processada.ContentType, conteudo);
                fotos.Add(new FotoGaleriaInput { Url = salvo.Url, Legenda = null });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            await GravarGaleria(configs, uow, tid, fotos);
            return Ok(fotos);
        }

        /// <summary>
        /// Atualiza a lista (ordem, legendas, remoções). Fotos removidas que eram
        /// uploads da própria galeria são apagadas do storage após persistir.
        /// </summary>
        [HttpPut("api/v1/admin/vitrine/galeria")]
        [Authorize(Policy = "AdminTenant")]
        public async Task<IActionResult> SalvarGaleria(
            [FromServices] Core.Interfaces.Services.IFotoStorage storage,
            [FromServices] IConfiguracaoTenantRepository configs,
            [FromServices] IUnitOfWork uow,
            [FromServices] ITenantContext ctx,
            [FromBody] List<FotoGaleriaInput> fotos)
        {
            var tid = RequireTenantId(ctx);
            fotos ??= new List<FotoGaleriaInput>();
            if (fotos.Count > MaxFotosGaleria)
                return BadRequest(new { message = $"A galeria comporta no máximo {MaxFotosGaleria} fotos." });
            foreach (var f in fotos)
            {
                if (string.IsNullOrWhiteSpace(f.Url))
                    return BadRequest(new { message = "Foto sem URL." });
                if ((f.Legenda?.Length ?? 0) > MaxLegenda)
                    return BadRequest(new { message = $"Legenda com no máximo {MaxLegenda} caracteres." });
                f.Legenda = string.IsNullOrWhiteSpace(f.Legenda) ? null : f.Legenda.Trim();
            }

            var anteriores = await LerGaleria(configs, tid);
            await GravarGaleria(configs, uow, tid, fotos);

            // Limpa arquivos órfãos — só uploads de galeria DESTE tenant; URL externa
            // que o lojista tenha colado nunca é tocada.
            var atuais = fotos.Select(f => f.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var antiga in anteriores)
            {
                if (!atuais.Contains(antiga.Url)
                    && antiga.Url.Contains($"/{tid}/vitrine/galeria-", StringComparison.OrdinalIgnoreCase))
                {
                    await storage.RemoverAsync(antiga.Url);
                }
            }

            return Ok(fotos);
        }

        [HttpGet("api/v1/t/{slug}/galeria")]
        [AllowAnonymous]
        public async Task<IActionResult> ListarGaleriaPublica(
            [FromServices] IConfiguracaoTenantRepository configs,
            [FromServices] ITenantContext ctx,
            string slug)
        {
            var tid = RequireTenantId(ctx);
            return Ok(await LerGaleria(configs, tid));
        }

        private static async Task<List<FotoGaleriaInput>> LerGaleria(IConfiguracaoTenantRepository configs, int tenantId)
        {
            var cfg = await configs.GetByChaveAsync(tenantId, ChaveGaleria);
            if (cfg == null || string.IsNullOrWhiteSpace(cfg.CfgValor)) return new List<FotoGaleriaInput>();
            try
            {
                return JsonSerializer.Deserialize<List<FotoGaleriaInput>>(cfg.CfgValor, JsonOpts)
                    ?? new List<FotoGaleriaInput>();
            }
            catch (JsonException)
            {
                return new List<FotoGaleriaInput>();
            }
        }

        private static async Task GravarGaleria(IConfiguracaoTenantRepository configs, IUnitOfWork uow,
            int tenantId, List<FotoGaleriaInput> fotos)
        {
            var json = JsonSerializer.Serialize(fotos, JsonOpts);
            var existente = await configs.GetByChaveAsync(tenantId, ChaveGaleria);
            if (existente == null)
            {
                await configs.CreateAsync(new ConfiguracaoTenant(tenantId, ChaveGaleria, json, "vitrine", sensivel: false));
            }
            else
            {
                existente.AlterarValor(json);
                await configs.UpdateAsync(existente);
                await uow.SaveChangesAsync();
            }
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

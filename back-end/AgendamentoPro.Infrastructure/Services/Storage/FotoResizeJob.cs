using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Processing;

namespace AgendamentoPro.Infrastructure.Services.Storage
{
    /// <summary>
    /// Job Hangfire que redimensiona imagens grandes em background e atualiza o
    /// `FotTamanhoBytes` da entidade para refletir o tamanho real do arquivo final.
    /// Sem essa atualização, o banco continuaria reportando o tamanho do upload
    /// original (que pode ser muito maior que o arquivo após o resize).
    /// </summary>
    public class FotoResizeJob
    {
        private const int LadoMaximo = 1920;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FotoResizeJob> _logger;

        public FotoResizeJob(IServiceScopeFactory scopeFactory, ILogger<FotoResizeJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 30, 120 })]
        public async Task ExecutarAsync(int fotoId, int tenantId, string urlRelativa)
        {
            using var scope = _scopeFactory.CreateScope();

            // CRÍTICO em modo PerTenant: sem setar o TenantContext, a factory do
            // DbContext resolve a connection do banco SHARED, e a foto (que vive
            // no banco do tenant) não é encontrada — o resize roda mas o
            // FotTamanhoBytes nunca é atualizado. Em modo Shared, o setter é no-op.
            var tCtx = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tCtx.SetTenant(tenantId, slug: null);

            var storage = scope.ServiceProvider.GetRequiredService<IFotoStorage>();
            var caminho = storage.ResolverCaminho(urlRelativa);
            if (caminho == null || !File.Exists(caminho))
            {
                _logger.LogWarning("FotoResizeJob: arquivo não encontrado para foto {FotoId} ({Url})", fotoId, urlRelativa);
                return;
            }

            await RedimensionarSeNecessarioAsync(caminho);

            var tamanhoFinal = new FileInfo(caminho).Length;
            var fotos = scope.ServiceProvider.GetRequiredService<IFotoAgendamentoRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<Core.Interfaces.Database.Common.IUnitOfWork>();
            var foto = await fotos.GetByIdAsync(fotoId, tenantId);
            if (foto == null) return;
            foto.AtualizarTamanho(tamanhoFinal);
            await uow.SaveChangesAsync();
        }

        private static async Task RedimensionarSeNecessarioAsync(string caminho)
        {
            using var img = await SixLabors.ImageSharp.Image.LoadAsync(caminho);
            if (img.Width <= LadoMaximo && img.Height <= LadoMaximo) return;
            var ratio = (double)LadoMaximo / Math.Max(img.Width, img.Height);
            var novoW = (int)Math.Round(img.Width * ratio);
            var novoH = (int)Math.Round(img.Height * ratio);
            img.Mutate(x => x.Resize(novoW, novoH));

            var ext = Path.GetExtension(caminho).ToLowerInvariant();
            SixLabors.ImageSharp.Formats.IImageEncoder encoder = ext switch
            {
                ".jpg" or ".jpeg" => new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 85 },
                ".png" => new SixLabors.ImageSharp.Formats.Png.PngEncoder(),
                ".webp" => new SixLabors.ImageSharp.Formats.Webp.WebpEncoder { Quality = 85 },
                ".gif" => new SixLabors.ImageSharp.Formats.Gif.GifEncoder(),
                _ => new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 85 }
            };
            await using var fs = File.Create(caminho);
            await img.SaveAsync(fs, encoder);
        }
    }
}

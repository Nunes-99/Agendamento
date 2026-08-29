using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace AgendamentoPro.Infrastructure.Services.Storage
{
    /// <summary>
    /// Processamento síncrono no upload (diferente do FotoResizeJob, que roda em
    /// background sobre o arquivo em disco): a vitrine aplica a imagem na hora e o
    /// processamento em memória funciona igual para storage local e S3.
    /// </summary>
    public class VitrineImagemProcessor : IVitrineImagemProcessor
    {
        private const long TamanhoMaxBytes = 10 * 1024 * 1024; // mesmo teto do storage
        private const int LogoLadoMax = 512;
        private const int BannerLarguraMax = 1920;
        private const double BannerProporcao = 3.0; // capa 3:1 (hero largo e baixo)
        private const int FaviconLado = 128;
        private const int GaleriaLadoMax = 1600;

        private readonly ILogger<VitrineImagemProcessor> _logger;

        public VitrineImagemProcessor(ILogger<VitrineImagemProcessor> logger)
        {
            _logger = logger;
        }

        public async Task<VitrineImagemProcessada> ProcessarAsync(string tipo, Stream original,
            CancellationToken ct = default)
        {
            // Buffer em memória: o stream do multipart não é seekable e o decode
            // precisa reler; o teto evita estourar memória antes do limite do storage.
            using var bruto = new MemoryStream();
            var buffer = new byte[81920];
            int lidos;
            long total = 0;
            while ((lidos = await original.ReadAsync(buffer, ct)) > 0)
            {
                total += lidos;
                if (total > TamanhoMaxBytes)
                    throw new InvalidOperationException("Arquivo excede o tamanho máximo permitido (10 MB).");
                await bruto.WriteAsync(buffer.AsMemory(0, lidos), ct);
            }
            bruto.Position = 0;

            Image img;
            try
            {
                img = await Image.LoadAsync(bruto, ct);
            }
            catch (UnknownImageFormatException)
            {
                throw new InvalidOperationException("O arquivo não é uma imagem válida (jpg, png, webp ou gif).");
            }
            catch (InvalidImageContentException)
            {
                throw new InvalidOperationException("A imagem está corrompida e não pôde ser lida.");
            }

            using (img)
            {
                var formato = img.Metadata.DecodedImageFormat;

                // GIF (possivelmente animado) passa intocado: redimensionar quadro a
                // quadro não vale a complexidade para logo/banner.
                if (formato is GifFormat)
                {
                    bruto.Position = 0;
                    return new VitrineImagemProcessada(new MemoryStream(bruto.ToArray()), "image/gif", ".gif");
                }

                switch (tipo)
                {
                    case "logo":
                        // Só reduz: logo menor que o teto fica como veio.
                        if (img.Width > LogoLadoMax || img.Height > LogoLadoMax)
                            img.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Size = new Size(LogoLadoMax, LogoLadoMax),
                                Mode = ResizeMode.Max
                            }));
                        break;

                    case "banner":
                    {
                        // Maior recorte 3:1 possível SEM ampliar, limitado a 1920 de largura.
                        var larguraCrop = Math.Min(img.Width, (int)(img.Height * BannerProporcao));
                        var largura = Math.Min(larguraCrop, BannerLarguraMax);
                        var altura = Math.Max(1, (int)Math.Round(largura / BannerProporcao));
                        if (largura != img.Width || altura != img.Height)
                            img.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Size = new Size(largura, altura),
                                Mode = ResizeMode.Crop,
                                Position = AnchorPositionMode.Center
                            }));
                        break;
                    }

                    case "favicon":
                    {
                        var lado = Math.Min(FaviconLado, Math.Min(img.Width, img.Height));
                        img.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(lado, lado),
                            Mode = ResizeMode.Crop,
                            Position = AnchorPositionMode.Center
                        }));
                        break;
                    }

                    case "galeria":
                        // Foto do espaço: só reduz para caber em 1600² — sem crop,
                        // enquadramento é escolha do lojista (cropper no front).
                        if (img.Width > GaleriaLadoMax || img.Height > GaleriaLadoMax)
                            img.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Size = new Size(GaleriaLadoMax, GaleriaLadoMax),
                                Mode = ResizeMode.Max
                            }));
                        break;
                }

                // Favicon sempre PNG (transparência + suporte universal nas abas);
                // os demais mantêm o formato de origem.
                (IImageEncoder encoder, string contentType, string ext) = tipo == "favicon"
                    ? (new PngEncoder(), "image/png", ".png")
                    : formato switch
                    {
                        PngFormat => (new PngEncoder(), "image/png", ".png"),
                        WebpFormat => ((IImageEncoder)new WebpEncoder { Quality = 85 }, "image/webp", ".webp"),
                        _ => (new JpegEncoder { Quality = 85 }, "image/jpeg", ".jpg")
                    };

                var saida = new MemoryStream();
                await img.SaveAsync(saida, encoder, ct);
                saida.Position = 0;

                _logger.LogInformation("Imagem de vitrine ({Tipo}) processada: {W}x{H}, {Bytes} bytes",
                    tipo, img.Width, img.Height, saida.Length);
                return new VitrineImagemProcessada(saida, contentType, ext);
            }
        }
    }
}

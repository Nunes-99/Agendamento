using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Processing;

namespace AgendamentoPro.Infrastructure.Services.Storage
{
    /// <summary>
    /// Storage local em disco. Escreve em UPLOADS_PATH/{tenantId}/{agendamentoId}/{guid}.{ext}
    /// e retorna URL relativa /uploads/{tenantId}/{agendamentoId}/{guid}.{ext}.
    /// O frontend deve servir essa rota via static files (Program.cs UseStaticFiles).
    /// </summary>
    public class LocalFotoStorage : IFotoStorage
    {
        private const long TamanhoMaxBytes = 10 * 1024 * 1024; // 10 MB
        private static readonly HashSet<string> ContentTypesPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif"
        };
        private static readonly HashSet<string> ExtensoesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly string _basePath;
        private readonly ILogger<LocalFotoStorage> _logger;

        public LocalFotoStorage(IConfiguration config, ILogger<LocalFotoStorage> logger)
        {
            _logger = logger;
            _basePath = Environment.GetEnvironmentVariable("UPLOADS_PATH")
                ?? config["Uploads:Path"]
                ?? Path.Combine(AppContext.BaseDirectory, "uploads");
            Directory.CreateDirectory(_basePath);
        }

        public async Task<string> SalvarAsync(int tenantId, int agendamentoId,
            string nomeOriginal, string contentType, Stream conteudo, CancellationToken ct = default)
        {
            if (tenantId <= 0 || agendamentoId <= 0)
                throw new ArgumentException("Tenant e Agendamento são obrigatórios.");
            if (!ContentTypesPermitidos.Contains(contentType ?? string.Empty))
                throw new InvalidOperationException($"Content-type '{contentType}' não permitido.");

            var ext = Path.GetExtension(nomeOriginal ?? string.Empty).ToLowerInvariant();
            if (!ExtensoesPermitidas.Contains(ext))
                throw new InvalidOperationException($"Extensão '{ext}' não permitida.");

            var dir = Path.Combine(_basePath, tenantId.ToString(), agendamentoId.ToString());
            Directory.CreateDirectory(dir);

            var nome = $"{Guid.NewGuid():N}{ext}";
            var caminho = Path.Combine(dir, nome);

            try
            {
                await using var fs = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    bufferSize: 81920, useAsync: true);
                long total = 0;
                var buffer = new byte[81920];
                int lidos;
                while ((lidos = await conteudo.ReadAsync(buffer, ct)) > 0)
                {
                    total += lidos;
                    if (total > TamanhoMaxBytes)
                        throw new InvalidOperationException("Arquivo excede o tamanho máximo permitido (10 MB).");
                    await fs.WriteAsync(buffer.AsMemory(0, lidos), ct);
                }
            }
            catch
            {
                // Garante que o stream foi fechado pelo `await using` antes de tentar remover o arquivo parcial
                try { if (File.Exists(caminho)) File.Delete(caminho); } catch { /* best-effort */ }
                throw;
            }

            // Resize automático: imagens > 1920px no eixo maior são reduzidas para 1920
            // mantendo aspect ratio. Reduz banda de download em 70-90% sem perda visual.
            try { await RedimensionarSeNecessarioAsync(caminho); }
            catch { /* falha silenciosa: original já está no disco */ }

            // URL relativa servida via /uploads via UseStaticFiles
            return $"/uploads/{tenantId}/{agendamentoId}/{nome}";
        }

        private static async Task RedimensionarSeNecessarioAsync(string caminho)
        {
            const int LadoMaximo = 1920;
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

        public Task RemoverAsync(string urlRelativa, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(urlRelativa)) return Task.CompletedTask;
            var caminhoRelativo = urlRelativa.TrimStart('/').Replace("uploads/", string.Empty, StringComparison.OrdinalIgnoreCase);
            var caminho = Path.Combine(_basePath, caminhoRelativo);
            // Defesa básica contra path traversal
            var fullBase = Path.GetFullPath(_basePath);
            var fullTarget = Path.GetFullPath(caminho);
            if (!fullTarget.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("RemoverAsync: tentativa de path traversal bloqueada: {Url}", urlRelativa);
                return Task.CompletedTask;
            }
            try { if (File.Exists(fullTarget)) File.Delete(fullTarget); }
            catch (Exception ex) { _logger.LogWarning(ex, "Falha ao remover {Caminho}", fullTarget); }
            return Task.CompletedTask;
        }
    }
}

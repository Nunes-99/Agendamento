using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Infrastructure.Services.Storage
{
    /// <summary>
    /// Storage local em disco. Escreve em UPLOADS_PATH/{tenantId}/{agendamentoId}/{guid}.{ext}
    /// e retorna URL relativa /uploads/{tenantId}/{agendamentoId}/{guid}.{ext}.
    /// O frontend deve servir essa rota via static files (Program.cs UseStaticFiles).
    /// O resize de imagens grandes é enfileirado pelo `FotoAgendamentoUseCase` em
    /// `FotoResizeJob` — este storage apenas grava o original.
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

        public async Task<FotoSalvaResult> SalvarAsync(int tenantId, int agendamentoId,
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

            long totalGravado;
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
                totalGravado = total;
            }
            catch
            {
                // Garante que o stream foi fechado pelo `await using` antes de tentar remover o arquivo parcial
                try { if (File.Exists(caminho)) File.Delete(caminho); } catch { /* best-effort */ }
                throw;
            }

            var url = $"/uploads/{tenantId}/{agendamentoId}/{nome}";
            return new FotoSalvaResult(url, totalGravado);
        }

        public Task RemoverAsync(string urlRelativa, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(urlRelativa)) return Task.CompletedTask;
            var fullTarget = ResolverCaminhoSeguro(urlRelativa);
            if (fullTarget == null) return Task.CompletedTask;
            try { if (File.Exists(fullTarget)) File.Delete(fullTarget); }
            catch (Exception ex) { _logger.LogWarning(ex, "Falha ao remover {Caminho}", fullTarget); }
            return Task.CompletedTask;
        }

        public string ResolverCaminho(string urlRelativa) => ResolverCaminhoSeguro(urlRelativa);

        /// <summary>Resolve com defesa básica contra path traversal; retorna null se inválida.</summary>
        private string ResolverCaminhoSeguro(string urlRelativa)
        {
            if (string.IsNullOrWhiteSpace(urlRelativa)) return null;
            var caminhoRelativo = urlRelativa.TrimStart('/').Replace("uploads/", string.Empty, StringComparison.OrdinalIgnoreCase);
            var caminho = Path.Combine(_basePath, caminhoRelativo);
            var fullBase = Path.GetFullPath(_basePath);
            var fullTarget = Path.GetFullPath(caminho);
            if (!fullTarget.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal bloqueado: {Url}", urlRelativa);
                return null;
            }
            return fullTarget;
        }
    }
}

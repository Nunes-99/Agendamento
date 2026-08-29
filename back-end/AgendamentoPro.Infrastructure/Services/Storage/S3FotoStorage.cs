using AgendamentoPro.Core.Interfaces.Services;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Infrastructure.Services.Storage
{
    /// <summary>
    /// Storage de fotos em S3 (ou S3-compatível: MinIO, Backblaze B2, R2, etc).
    /// Key no bucket: {tenantId}/{agendamentoId}/{guid}.{ext}. URL retornada aponta
    /// para o endpoint público do bucket (ou CloudFront se configurado).
    ///
    /// <para>Variáveis de ambiente:</para>
    /// <list type="bullet">
    /// <item>S3_BUCKET — nome do bucket (obrigatório)</item>
    /// <item>S3_REGION — ex.: "us-east-1" (obrigatório p/ AWS; opcional p/ MinIO)</item>
    /// <item>S3_ENDPOINT — sobrescreve endpoint (MinIO/B2/R2). Ex: http://minio:9000</item>
    /// <item>S3_ACCESS_KEY / S3_SECRET_KEY — credenciais explícitas (default = chain provider AWS)</item>
    /// <item>S3_PUBLIC_BASE_URL — base pública das URLs (ex: https://cdn.exemplo.com).
    ///   Se omitido, deriva: AWS = https://{bucket}.s3.{region}.amazonaws.com;
    ///   custom = {endpoint}/{bucket}</item>
    /// <item>S3_FORCE_PATH_STYLE — "true" para MinIO/B2 (default false)</item>
    /// </list>
    ///
    /// <para>Observação sobre resize: o `FotoResizeJob` precisa de um caminho local
    /// (ImageSharp lê do disco). Em modo S3, <see cref="ResolverCaminho"/> retorna
    /// null e o job pula o resize, deixando o original. Para resize após upload:
    /// configure S3 Event → Lambda/Hangfire externo que baixa, redimensiona, sobe
    /// de volta. Frontend pode usar parâmetros de CloudFront/Imgix para resize on-the-fly.</para>
    /// </summary>
    public class S3FotoStorage : IFotoStorage
    {
        private const long TamanhoMaxBytes = 10 * 1024 * 1024;
        private static readonly HashSet<string> ContentTypesPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif"
        };
        private static readonly HashSet<string> ExtensoesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        private readonly string _publicBaseUrl;
        private readonly ILogger<S3FotoStorage> _logger;

        public S3FotoStorage(IAmazonS3 s3, IConfiguration config, ILogger<S3FotoStorage> logger)
        {
            _s3 = s3;
            _logger = logger;
            _bucket = Environment.GetEnvironmentVariable("S3_BUCKET")
                ?? config["Storage:S3:Bucket"]
                ?? throw new InvalidOperationException("S3_BUCKET é obrigatório quando STORAGE_PROVIDER=s3.");

            var explicitBase = Environment.GetEnvironmentVariable("S3_PUBLIC_BASE_URL")
                ?? config["Storage:S3:PublicBaseUrl"];
            if (!string.IsNullOrWhiteSpace(explicitBase))
            {
                _publicBaseUrl = explicitBase.TrimEnd('/');
            }
            else
            {
                var endpoint = Environment.GetEnvironmentVariable("S3_ENDPOINT") ?? config["Storage:S3:Endpoint"];
                var region = Environment.GetEnvironmentVariable("S3_REGION") ?? config["Storage:S3:Region"] ?? "us-east-1";
                _publicBaseUrl = !string.IsNullOrWhiteSpace(endpoint)
                    ? $"{endpoint.TrimEnd('/')}/{_bucket}"
                    : $"https://{_bucket}.s3.{region}.amazonaws.com";
            }
        }

        public Task<FotoSalvaResult> SalvarAsync(int tenantId, int agendamentoId,
            string nomeOriginal, string contentType, Stream conteudo, CancellationToken ct = default)
        {
            if (tenantId <= 0 || agendamentoId <= 0)
                throw new ArgumentException("Tenant e Agendamento são obrigatórios.");
            return SalvarComKeyAsync($"{tenantId}/{agendamentoId}", prefixoNome: null,
                nomeOriginal, contentType, conteudo, ct);
        }

        public Task<FotoSalvaResult> SalvarVitrineAsync(int tenantId, string tipo,
            string nomeOriginal, string contentType, Stream conteudo, CancellationToken ct = default)
        {
            if (tenantId <= 0) throw new ArgumentException("Tenant é obrigatório.");
            if (string.IsNullOrWhiteSpace(tipo)) throw new ArgumentException("Tipo é obrigatório.");
            return SalvarComKeyAsync($"{tenantId}/vitrine", prefixoNome: tipo,
                nomeOriginal, contentType, conteudo, ct);
        }

        private async Task<FotoSalvaResult> SalvarComKeyAsync(string keyPrefixo, string prefixoNome,
            string nomeOriginal, string contentType, Stream conteudo, CancellationToken ct)
        {
            if (!ContentTypesPermitidos.Contains(contentType ?? string.Empty))
                throw new InvalidOperationException($"Content-type '{contentType}' não permitido.");

            var ext = Path.GetExtension(nomeOriginal ?? string.Empty).ToLowerInvariant();
            if (!ExtensoesPermitidas.Contains(ext))
                throw new InvalidOperationException($"Extensão '{ext}' não permitida.");

            // Buffer no MemoryStream para impor limite de tamanho antes de gastar PUT.
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            int lidos;
            long total = 0;
            while ((lidos = await conteudo.ReadAsync(buffer, ct)) > 0)
            {
                total += lidos;
                if (total > TamanhoMaxBytes)
                    throw new InvalidOperationException("Arquivo excede o tamanho máximo permitido (10 MB).");
                await ms.WriteAsync(buffer.AsMemory(0, lidos), ct);
            }
            ms.Position = 0;

            var nome = string.IsNullOrEmpty(prefixoNome)
                ? $"{Guid.NewGuid():N}{ext}"
                : $"{prefixoNome}-{Guid.NewGuid():N}{ext}";
            var key = $"{keyPrefixo}/{nome}";

            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = ms,
                ContentType = contentType,
                DisablePayloadSigning = true
            }, ct);

            var url = $"{_publicBaseUrl}/{key}";
            return new FotoSalvaResult(url, total);
        }

        public async Task RemoverAsync(string urlRelativa, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(urlRelativa)) return;
            var key = ExtrairKey(urlRelativa);
            if (key == null) return;
            try
            {
                await _s3.DeleteObjectAsync(new DeleteObjectRequest
                {
                    BucketName = _bucket,
                    Key = key
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao remover objeto S3 {Key}", key);
            }
        }

        /// <summary>
        /// Não há caminho local para objetos S3 — sempre retorna null. O
        /// `FotoResizeJob` trata isso como skip silencioso.
        /// </summary>
        public string ResolverCaminho(string urlRelativa) => null;

        private string ExtrairKey(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (url.StartsWith(_publicBaseUrl, StringComparison.OrdinalIgnoreCase))
                return url.Substring(_publicBaseUrl.Length).TrimStart('/');
            // Tolerância a URLs já no formato relativo "tenantId/agendamentoId/foto.jpg"
            return url.TrimStart('/');
        }
    }
}

using AgendamentoPro.Infrastructure.Services.Storage;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;

namespace AgendamentoPro.Tests.Services
{
    /// <summary>
    /// Cobre upload/delete/URL building do S3FotoStorage usando mock do IAmazonS3.
    /// Não exige bucket real.
    /// </summary>
    public class S3FotoStorageTests
    {
        private readonly Mock<IAmazonS3> _s3 = new();

        public S3FotoStorageTests()
        {
            Environment.SetEnvironmentVariable("S3_BUCKET", "agendpro-test");
            Environment.SetEnvironmentVariable("S3_REGION", "us-east-1");
            Environment.SetEnvironmentVariable("S3_PUBLIC_BASE_URL", "https://cdn.exemplo.com");
        }

        private S3FotoStorage Criar()
        {
            var cfg = new ConfigurationBuilder().AddInMemoryCollection().Build();
            return new S3FotoStorage(_s3.Object, cfg, new NullLogger<S3FotoStorage>());
        }

        private static MemoryStream FakeImage(int bytes = 1024)
            => new(Enumerable.Repeat((byte)0xFF, bytes).ToArray());

        [Fact]
        public async Task Salvar_ConteudoValido_FazPutObjectEretornaUrlPublica()
        {
            PutObjectRequest capturado = null;
            _s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutObjectRequest, CancellationToken>((req, _) => capturado = req)
                .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

            var storage = Criar();
            using var stream = FakeImage(2048);
            var resultado = await storage.SalvarAsync(7, 42, "foto.jpg", "image/jpeg", stream);

            resultado.Url.Should().StartWith("https://cdn.exemplo.com/7/42/")
                .And.EndWith(".jpg");
            resultado.TamanhoBytes.Should().Be(2048);
            capturado!.BucketName.Should().Be("agendpro-test");
            capturado.Key.Should().StartWith("7/42/").And.EndWith(".jpg");
            capturado.ContentType.Should().Be("image/jpeg");
        }

        [Fact]
        public async Task Salvar_ContentTypeNaoPermitido_LancaSemChamarS3()
        {
            var storage = Criar();
            using var stream = FakeImage();
            Func<Task> act = () => storage.SalvarAsync(1, 1, "x.exe", "application/octet-stream", stream);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _s3.Verify(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Salvar_ArquivoMaiorQueLimite_LancaSemChamarS3()
        {
            var storage = Criar();
            using var stream = new MemoryStream(Enumerable.Repeat((byte)0xFF, 11 * 1024 * 1024).ToArray());
            Func<Task> act = () => storage.SalvarAsync(1, 1, "big.jpg", "image/jpeg", stream);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*tamanho máximo*");
            _s3.Verify(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Remover_UrlValida_ExtraiKeyECoroaDeleteObject()
        {
            DeleteObjectRequest capturado = null;
            _s3.Setup(s => s.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
                .Callback<DeleteObjectRequest, CancellationToken>((req, _) => capturado = req)
                .ReturnsAsync(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });

            var storage = Criar();
            await storage.RemoverAsync("https://cdn.exemplo.com/7/42/abc123.jpg");

            capturado!.BucketName.Should().Be("agendpro-test");
            capturado.Key.Should().Be("7/42/abc123.jpg");
        }

        [Fact]
        public async Task Remover_FalhaS3_LogaENaoLanca()
        {
            _s3.Setup(s => s.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonS3Exception("offline"));

            var storage = Criar();
            Func<Task> act = () => storage.RemoverAsync("https://cdn.exemplo.com/1/2/x.jpg");
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public void ResolverCaminho_SempreNull()
        {
            // S3 não tem caminho local — FotoResizeJob trata null como skip.
            var storage = Criar();
            storage.ResolverCaminho("https://cdn.exemplo.com/1/2/x.jpg").Should().BeNull();
        }

        [Fact]
        public void Construtor_SemBucket_Lanca()
        {
            Environment.SetEnvironmentVariable("S3_BUCKET", null);
            var cfg = new ConfigurationBuilder().AddInMemoryCollection().Build();
            Action act = () => new S3FotoStorage(_s3.Object, cfg, new NullLogger<S3FotoStorage>());
            act.Should().Throw<InvalidOperationException>().WithMessage("*S3_BUCKET*");
            // Restaura para outros testes da classe (caso ordem mude)
            Environment.SetEnvironmentVariable("S3_BUCKET", "agendpro-test");
        }
    }
}

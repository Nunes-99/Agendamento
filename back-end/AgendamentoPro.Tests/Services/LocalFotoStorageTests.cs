using AgendamentoPro.Infrastructure.Services.Storage;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgendamentoPro.Tests.Services
{
    public class LocalFotoStorageTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly LocalFotoStorage _storage;

        public LocalFotoStorageTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "agp-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            Environment.SetEnvironmentVariable("UPLOADS_PATH", _tempDir);
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            _storage = new LocalFotoStorage(config, new NullLogger<LocalFotoStorage>());
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("UPLOADS_PATH", null);
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
        }

        private static MemoryStream FakeImage(int bytes = 1024) => new(Enumerable.Repeat((byte)0xFF, bytes).ToArray());

        [Fact]
        public async Task Salvar_ArquivoValido_RetornaUrlERealmenteEscreve()
        {
            using var stream = FakeImage();
            var resultado = await _storage.SalvarAsync(7, 42, "foto.jpg", "image/jpeg", stream);

            resultado.Url.Should().StartWith("/uploads/7/42/").And.EndWith(".jpg");
            resultado.TamanhoBytes.Should().Be(1024);

            var caminho = Path.Combine(_tempDir, resultado.Url.TrimStart('/').Replace("uploads/", string.Empty));
            File.Exists(caminho).Should().BeTrue();
            new FileInfo(caminho).Length.Should().Be(1024);
        }

        [Fact]
        public async Task Salvar_ContentTypeNaoPermitido_Lanca()
        {
            using var stream = FakeImage();
            Func<Task> act = () => _storage.SalvarAsync(1, 1, "x.exe", "application/octet-stream", stream);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Content-type*");
        }

        [Fact]
        public async Task Salvar_ExtensaoNaoPermitida_Lanca()
        {
            using var stream = FakeImage();
            Func<Task> act = () => _storage.SalvarAsync(1, 1, "x.svg", "image/jpeg", stream);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Extens*");
        }

        [Fact]
        public async Task Salvar_ArquivoMaiorQueLimite_Lanca_ENaoDeixaArquivoOrfao()
        {
            using var stream = new MemoryStream(Enumerable.Repeat((byte)0xFF, 11 * 1024 * 1024).ToArray());
            Func<Task> act = () => _storage.SalvarAsync(1, 1, "big.jpg", "image/jpeg", stream);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*tamanho máximo*");
            Directory.GetFiles(_tempDir, "*.jpg", SearchOption.AllDirectories).Should().BeEmpty();
        }

        [Fact]
        public async Task Remover_PathTraversal_Bloqueado()
        {
            var fora = Path.Combine(Path.GetDirectoryName(_tempDir)!, "fora-do-base.txt");
            await File.WriteAllTextAsync(fora, "secret");

            await _storage.RemoverAsync("/uploads/../fora-do-base.txt");
            File.Exists(fora).Should().BeTrue("remover não deve apagar arquivos fora do diretório de uploads");

            File.Delete(fora);
        }

        [Fact]
        public async Task Remover_ArquivoExistente_RemoveDoDisco()
        {
            using var stream = FakeImage();
            var resultado = await _storage.SalvarAsync(1, 1, "x.jpg", "image/jpeg", stream);
            await _storage.RemoverAsync(resultado.Url);
            var caminho = Path.Combine(_tempDir, resultado.Url.TrimStart('/').Replace("uploads/", string.Empty));
            File.Exists(caminho).Should().BeFalse();
        }

        [Fact]
        public void ResolverCaminho_PathTraversal_RetornaNull()
        {
            _storage.ResolverCaminho("/uploads/../escape.jpg").Should().BeNull();
        }

        [Fact]
        public async Task ResolverCaminho_PathValido_ApontaParaArquivoExistente()
        {
            using var stream = FakeImage();
            var resultado = await _storage.SalvarAsync(1, 1, "x.jpg", "image/jpeg", stream);
            var caminho = _storage.ResolverCaminho(resultado.Url);
            caminho.Should().NotBeNull();
            File.Exists(caminho).Should().BeTrue();
        }
    }
}

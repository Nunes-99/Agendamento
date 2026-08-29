using AgendamentoPro.Infrastructure.Services.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace AgendamentoPro.Tests.Services
{
    public class VitrineImagemProcessorTests
    {
        private readonly VitrineImagemProcessor _processador = new(NullLogger<VitrineImagemProcessor>.Instance);

        private static MemoryStream ImagemPng(int largura, int altura)
        {
            using var img = new Image<Rgb24>(largura, altura);
            var ms = new MemoryStream();
            img.Save(ms, new PngEncoder());
            ms.Position = 0;
            return ms;
        }

        private static MemoryStream ImagemJpeg(int largura, int altura)
        {
            using var img = new Image<Rgb24>(largura, altura);
            var ms = new MemoryStream();
            img.Save(ms, new JpegEncoder());
            ms.Position = 0;
            return ms;
        }

        private static async Task<(int W, int H)> Dimensoes(Stream s)
        {
            using var img = await Image.LoadAsync(s);
            return (img.Width, img.Height);
        }

        [Fact]
        public async Task Banner_grande_vira_capa_3x1_com_largura_maxima()
        {
            using var entrada = ImagemJpeg(4000, 2000);
            var r = await _processador.ProcessarAsync("banner", entrada);
            var (w, h) = await Dimensoes(r.Conteudo);
            w.Should().Be(1920);
            h.Should().Be(640); // 1920 / 3
        }

        [Fact]
        public async Task Banner_pequeno_nao_e_ampliado()
        {
            // 900x600: maior recorte 3:1 sem ampliar é 900x300
            using var entrada = ImagemJpeg(900, 600);
            var r = await _processador.ProcessarAsync("banner", entrada);
            var (w, h) = await Dimensoes(r.Conteudo);
            w.Should().Be(900);
            h.Should().Be(300);
        }

        [Fact]
        public async Task Logo_grande_cabe_em_512_mantendo_proporcao()
        {
            using var entrada = ImagemPng(1024, 640);
            var r = await _processador.ProcessarAsync("logo", entrada);
            var (w, h) = await Dimensoes(r.Conteudo);
            w.Should().Be(512);
            h.Should().Be(320);
            r.Extensao.Should().Be(".png"); // formato de origem preservado
        }

        [Fact]
        public async Task Logo_pequeno_fica_como_veio()
        {
            using var entrada = ImagemPng(100, 80);
            var r = await _processador.ProcessarAsync("logo", entrada);
            var (w, h) = await Dimensoes(r.Conteudo);
            (w, h).Should().Be((100, 80));
        }

        [Fact]
        public async Task Favicon_vira_png_quadrado_128()
        {
            using var entrada = ImagemJpeg(600, 400);
            var r = await _processador.ProcessarAsync("favicon", entrada);
            var (w, h) = await Dimensoes(r.Conteudo);
            (w, h).Should().Be((128, 128));
            r.ContentType.Should().Be("image/png");
            r.Extensao.Should().Be(".png");
        }

        [Fact]
        public async Task Foto_de_galeria_grande_cabe_em_1600_sem_crop()
        {
            using var entrada = ImagemJpeg(3200, 2400);
            var r = await _processador.ProcessarAsync("galeria", entrada);
            var (w, h) = await Dimensoes(r.Conteudo);
            (w, h).Should().Be((1600, 1200)); // proporção 4:3 preservada
        }

        [Fact]
        public async Task Foto_de_galeria_pequena_fica_como_veio()
        {
            using var entrada = ImagemJpeg(800, 600);
            var r = await _processador.ProcessarAsync("galeria", entrada);
            var (w, h) = await Dimensoes(r.Conteudo);
            (w, h).Should().Be((800, 600));
        }

        [Fact]
        public async Task Bytes_que_nao_sao_imagem_sao_rejeitados()
        {
            // Extensão/content-type certos não bastam: o conteúdo precisa decodificar.
            using var falso = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            var acao = () => _processador.ProcessarAsync("logo", falso);
            await acao.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*não é uma imagem válida*");
        }
    }
}

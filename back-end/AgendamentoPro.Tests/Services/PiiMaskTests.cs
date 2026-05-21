using AgendamentoPro.Core.Common;
using FluentAssertions;

namespace AgendamentoPro.Tests.Services
{
    public class PiiMaskTests
    {
        [Theory]
        [InlineData("vitor@example.com", "v****@example.com")]
        [InlineData("a@b.com", "a@b.com")]            // local-part 1 char — mantém
        [InlineData("abc@x.com", "a**@x.com")]        // local-part 3 → 1 visível + 2 estrelas
        [InlineData("longoemail@dominio.com.br", "l****@dominio.com.br")]
        [InlineData("", "-")]
        [InlineData(null, "-")]
        public void Email_Mascara(string entrada, string esperado)
        {
            PiiMask.Email(entrada).Should().Be(esperado);
        }

        [Fact]
        public void Email_SemArroba_RetornaInputComoEsta()
        {
            PiiMask.Email("nao-e-email").Should().Be("nao-e-email");
        }

        [Theory]
        [InlineData("11999998888", "*******8888")]
        [InlineData("+55 11 99999-8888", "*********8888")]
        [InlineData("1234", "1234")]                  // ≤4 dígitos → não mascara
        [InlineData("", "-")]
        [InlineData(null, "-")]
        public void Telefone_Mascara(string entrada, string esperado)
        {
            PiiMask.Telefone(entrada).Should().Be(esperado);
        }
    }
}

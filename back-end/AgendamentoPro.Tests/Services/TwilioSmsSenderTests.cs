using AgendamentoPro.Infrastructure.Services.Sms;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgendamentoPro.Tests.Services
{
    public class TwilioSmsSenderTests
    {
        [Fact]
        public void Construtor_SemEnv_NaoAtivo()
        {
            // Garante limpos
            Environment.SetEnvironmentVariable("TWILIO_ACCOUNT_SID", null);
            Environment.SetEnvironmentVariable("TWILIO_AUTH_TOKEN", null);
            Environment.SetEnvironmentVariable("TWILIO_FROM_NUMBER", null);

            var cfg = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var s = new TwilioSmsSender(cfg, new NullLogger<TwilioSmsSender>());
            s.Ativo.Should().BeFalse();
        }

        [Fact]
        public async Task EnviarAsync_NaoAtivo_RetornaFalse()
        {
            var cfg = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var s = new TwilioSmsSender(cfg, new NullLogger<TwilioSmsSender>());
            (await s.EnviarAsync("11999999999", "msg")).Should().BeFalse();
        }

        [Theory]
        [InlineData("11999999999", "+5511999999999")]      // 11 dígitos: assume BR (DDD + 9 dígitos)
        [InlineData("(11) 99999-9999", "+5511999999999")]  // formatado BR
        [InlineData("1199999999", "+551199999999")]        // 10 dígitos (DDD + 8 — fixo BR)
        [InlineData("+5511999999999", "+5511999999999")]   // já em E.164
        [InlineData("+1 415 555 1234", "+14155551234")]    // US/E.164 limpo
        [InlineData("123456789012", "+123456789012")]      // 12 dígitos: assume já incluir código país
        public void NormalizarE164_FormatosVariados(string entrada, string esperado)
        {
            TwilioSmsSender.NormalizarE164(entrada).Should().Be(esperado);
        }

        [Fact]
        public void NormalizarE164_VazioOuNull_RetornaComoEsta()
        {
            TwilioSmsSender.NormalizarE164("").Should().Be("");
            TwilioSmsSender.NormalizarE164(null).Should().BeNull();
        }
    }
}

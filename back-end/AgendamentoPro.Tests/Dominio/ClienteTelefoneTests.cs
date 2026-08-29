using AgendamentoPro.Core.Entities.Clientes;
using FluentAssertions;
using Xunit;

namespace AgendamentoPro.Tests.Dominio
{
    /// <summary>
    /// O mesmo telefone chegava com máscara diferente por fluxo e duplicava o
    /// cadastro do cliente. A busca já normaliza; aqui garantimos que a GRAVAÇÃO
    /// também deixa o dado canônico.
    /// </summary>
    public class ClienteTelefoneTests
    {
        [Theory]
        [InlineData("(11) 99887-7665", "11998877665")]
        [InlineData("11998877665", "11998877665")]
        [InlineData("+55 11 99887-7665", "11998877665")]
        [InlineData("5511998877665", "11998877665")]
        [InlineData("11 9 9887 7665", "11998877665")]
        public void Telefone_e_gravado_so_com_digitos(string entrada, string esperado)
        {
            var c = new Cliente(1, "Fulano", null, entrada, null, null);
            c.CliTelefone.Should().Be(esperado);
        }

        [Fact]
        public void WhatsApp_tambem_e_normalizado()
        {
            var c = new Cliente(1, "Fulano", null, null, "(11) 3333-4444", null);
            c.CliWhatsApp.Should().Be("1133334444");
        }

        [Fact]
        public void Atualizar_normaliza_do_mesmo_jeito()
        {
            var c = new Cliente(1, "Fulano", null, "11998877665", null, null);
            c.Atualizar("Fulano", "f@x.com", "(11) 9 9887-7665", "+55 (11) 99887-7665", null, null);
            c.CliTelefone.Should().Be("11998877665");
            c.CliWhatsApp.Should().Be("11998877665");
        }

        [Fact]
        public void Numero_curto_nao_perde_o_ddi_falso_positivo()
        {
            // "5511" tem 4 dígitos: não deve virar "11" — o corte de DDI só vale
            // para números longos o bastante para ter DDI + DDD + número.
            var c = new Cliente(1, "Fulano", null, "5511", null, null);
            c.CliTelefone.Should().Be("5511");
        }
    }
}

using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Exceptions;
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
            // para números longos o bastante para ter DDI + DDD + número. A
            // normalização usada na BUSCA continua tolerante (não é ela que
            // decide se o dado entra); quem recusa é o construtor, logo abaixo.
            Cliente.NormalizarTelefone("5511").Should().Be("5511");
        }

        [Theory]
        [InlineData("119")]
        [InlineData("5511")]
        [InlineData("999999999999999")]
        [InlineData("telefone")]
        public void Telefone_incompleto_e_recusado(string entrada)
        {
            // Antes disto o formulário público aceitava qualquer coisa: o cadastro
            // nascia com um número que nunca receberia a confirmação.
            var criar = () => new Cliente(1, "Fulano", null, entrada, null, null);
            criar.Should().Throw<ClienteException>().WithMessage("*Telefone*");
        }

        [Theory]
        [InlineData("vitor")]
        [InlineData("vitor@")]
        [InlineData("vitor@dominio")]
        [InlineData("a b@x.com")]
        public void Email_invalido_e_recusado(string entrada)
        {
            // "vitor" era aceito e só estourava lá na cobrança do Mercado Pago
            // ("payer.email must be a valid email"), derrubando o agendamento.
            var criar = () => new Cliente(1, "Fulano", entrada, "11998877665", null, null);
            criar.Should().Throw<ClienteException>().WithMessage("*mail*");
        }

        [Fact]
        public void Email_valido_e_guardado_em_minusculas()
        {
            var c = new Cliente(1, "Fulano", "  Vitor@Email.COM ", "11998877665", null, null);
            c.CliEmail.Should().Be("vitor@email.com");
        }

        [Fact]
        public void Nome_acima_do_limite_da_coluna_e_recusado()
        {
            // O SQLite ignora HasMaxLength: sem esta checagem, um POST anônimo
            // gravaria um nome de qualquer tamanho.
            var criar = () => new Cliente(1, new string('a', 201), null, "11998877665", null, null);
            criar.Should().Throw<ClienteException>().WithMessage("*200*");
        }

        [Fact]
        public void Cpf_com_digito_verificador_errado_e_recusado()
        {
            var criar = () => new Cliente(1, "Fulano", null, "11998877665", null, "111.111.111-11");
            criar.Should().Throw<ClienteException>().WithMessage("*CPF*");
        }

        [Fact]
        public void Cpf_valido_e_guardado_so_com_digitos()
        {
            var c = new Cliente(1, "Fulano", null, "11998877665", null, "529.982.247-25");
            c.CliCpf.Should().Be("52998224725");
        }
    }
}

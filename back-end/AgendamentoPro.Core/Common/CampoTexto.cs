using System.Text.RegularExpressions;

namespace AgendamentoPro.Core.Common
{
    /// <summary>
    /// Saneamento e limite dos campos de texto que vêm de formulário público.
    ///
    /// O <c>HasMaxLength</c> do EF só vira restrição no SQL Server; no SQLite ele
    /// é decorativo. Ou seja: nada impedia um POST anônimo de gravar um nome de
    /// 1 MB, e um e-mail "vitor" era aceito e depois derrubava a cobrança no
    /// Mercado Pago ("payer.email must be a valid email"). O limite de verdade
    /// tem que estar aqui, no domínio, e não só na máscara da tela — a tela é
    /// conveniência, o formulário público é superfície de ataque.
    /// </summary>
    public static class CampoTexto
    {
        // Sem RFC 5322 completa de propósito: o que interessa é recusar o que
        // gateway e servidor de e-mail vão recusar depois, não bancar o juiz.
        private static readonly Regex EmailValido = new(
            @"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Tira espaços das pontas e devolve null quando sobra nada.</summary>
        public static string Limpar(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            return valor.Trim();
        }

        /// <summary>
        /// Campo opcional: limpa e recusa acima do limite, em vez de truncar em
        /// silêncio — o usuário precisa saber que o texto dele não coube.
        /// </summary>
        public static string Opcional(string valor, int maximo, string nomeCampo,
            Func<string, Exception> erro)
        {
            var limpo = Limpar(valor);
            if (limpo != null && limpo.Length > maximo)
                throw erro($"{nomeCampo} deve ter no máximo {maximo} caracteres.");
            return limpo;
        }

        /// <summary>Campo obrigatório com limite.</summary>
        public static string Obrigatorio(string valor, int maximo, string nomeCampo,
            Func<string, Exception> erro)
        {
            var limpo = Limpar(valor);
            if (limpo == null)
                throw erro($"{nomeCampo} é obrigatório.");
            if (limpo.Length > maximo)
                throw erro($"{nomeCampo} deve ter no máximo {maximo} caracteres.");
            return limpo;
        }

        /// <summary>
        /// E-mail opcional: normaliza para minúsculas e recusa formato inválido.
        /// Vazio continua sendo aceito — no agendamento o e-mail não é exigido.
        /// </summary>
        public static string Email(string valor, string nomeCampo, Func<string, Exception> erro,
            int maximo = 255)
        {
            var limpo = Limpar(valor);
            if (limpo == null) return null;
            if (limpo.Length > maximo)
                throw erro($"{nomeCampo} deve ter no máximo {maximo} caracteres.");
            if (!EmailValido.IsMatch(limpo))
                throw erro($"{nomeCampo} inválido. Use o formato nome@dominio.com.");
            return limpo.ToLowerInvariant();
        }

        /// <summary>Verdadeiro quando o texto tem cara de e-mail utilizável.</summary>
        public static bool PareceEmail(string valor)
            => !string.IsNullOrWhiteSpace(valor) && EmailValido.IsMatch(valor.Trim());

        /// <summary>
        /// Telefone brasileiro opcional: guarda só dígitos e exige 10 (fixo) ou
        /// 11 (celular) — assim "119" não passa e depois some no envio do SMS.
        /// </summary>
        public static string Telefone(string valor, string nomeCampo, Func<string, Exception> erro)
        {
            var limpo = Limpar(valor);
            if (limpo == null) return null;

            var digitos = new string(limpo.Where(char.IsDigit).ToArray());
            if (digitos.Length > 11 && digitos.StartsWith("55")) digitos = digitos[2..];
            if (digitos.Length is not (10 or 11))
                throw erro($"{nomeCampo} inválido. Informe DDD + número, como (11) 98888-7777.");
            return digitos;
        }

        /// <summary>
        /// CPF opcional: valida os dígitos verificadores. Sem isso, um CPF digitado
        /// errado só aparecia na hora de emitir a nota.
        /// </summary>
        public static string Cpf(string valor, string nomeCampo, Func<string, Exception> erro)
        {
            var limpo = Limpar(valor);
            if (limpo == null) return null;

            var d = new string(limpo.Where(char.IsDigit).ToArray());
            if (d.Length != 11 || d.Distinct().Count() == 1)
                throw erro($"{nomeCampo} inválido.");

            for (var posicao = 9; posicao <= 10; posicao++)
            {
                var soma = 0;
                for (var i = 0; i < posicao; i++)
                    soma += (d[i] - '0') * (posicao + 1 - i);
                var resto = soma * 10 % 11;
                if (resto == 10) resto = 0;
                if (resto != d[posicao] - '0')
                    throw erro($"{nomeCampo} inválido.");
            }
            return d;
        }
    }
}

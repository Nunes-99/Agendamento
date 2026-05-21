namespace AgendamentoPro.Core.Common
{
    /// <summary>
    /// Helpers para mascarar PII em logs. LGPD exige minimização — logs
    /// centralizados (Loki/ELK/Datadog) acumulam PII se não for tratado.
    /// </summary>
    public static class PiiMask
    {
        /// <summary>
        /// Mascara um email para "a***@domain.com". Retorna o input como está
        /// se ele não parecer um email. Vazio/null → "-".
        /// </summary>
        public static string Email(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "-";
            var at = raw.IndexOf('@');
            if (at <= 0) return raw;
            var localPart = raw[..at];
            var dominio = raw[(at + 1)..];
            var visivel = localPart.Length switch
            {
                0 or 1 => localPart,
                _ => localPart[0] + new string('*', Math.Min(localPart.Length - 1, 4))
            };
            return $"{visivel}@{dominio}";
        }

        /// <summary>
        /// Mascara um telefone mostrando os últimos 4 dígitos. "11999998888" → "*******8888".
        /// Vazio/null → "-".
        /// </summary>
        public static string Telefone(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "-";
            var digitos = new string(raw.Where(char.IsDigit).ToArray());
            if (digitos.Length <= 4) return digitos;
            return new string('*', digitos.Length - 4) + digitos[^4..];
        }
    }
}

using AgendamentoPro.Core.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace AgendamentoPro.Infrastructure.Services.Auth
{
    /// <summary>
    /// TOTP RFC 6238 - HMAC-SHA1, 6 dígitos, time-step 30s. Compatível com
    /// Google Authenticator, Authy, 1Password, Microsoft Authenticator.
    /// </summary>
    public class TotpService : ITotpService
    {
        private const int TimeStep = 30;
        private const int Digitos = 6;
        private const int JanelaSteps = 1; // ±30s tolerância
        private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public string GerarSecret()
        {
            var bytes = RandomNumberGenerator.GetBytes(20);
            return ParaBase32(bytes);
        }

        public string GerarOtpAuthUrl(string secretBase32, string emailUsuario, string emissor)
        {
            var emissorEnc = Uri.EscapeDataString(emissor);
            var emailEnc = Uri.EscapeDataString(emailUsuario);
            return $"otpauth://totp/{emissorEnc}:{emailEnc}?secret={secretBase32}&issuer={emissorEnc}&algorithm=SHA1&digits={Digitos}&period={TimeStep}";
        }

        public bool Verificar(string secretBase32, string codigo, DateTime agoraUtc)
            => VerificarERetornarStep(secretBase32, codigo, agoraUtc) >= 0;

        public long VerificarERetornarStep(string secretBase32, string codigo, DateTime agoraUtc)
        {
            if (string.IsNullOrWhiteSpace(secretBase32) || string.IsNullOrWhiteSpace(codigo)) return -1;
            if (!int.TryParse(codigo.Trim(), out _)) return -1;
            if (codigo.Length != Digitos) return -1;

            var key = DeBase32(secretBase32);
            if (key.Length == 0) return -1;

            var stepAtual = (long)((agoraUtc - DateTime.UnixEpoch).TotalSeconds / TimeStep);
            for (var offset = -JanelaSteps; offset <= JanelaSteps; offset++)
            {
                var step = stepAtual + offset;
                var esperado = GerarCodigo(key, step);
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(esperado), Encoding.UTF8.GetBytes(codigo)))
                {
                    return step;
                }
            }
            return -1;
        }

        private static string GerarCodigo(byte[] key, long step)
        {
            var stepBytes = BitConverter.GetBytes(step);
            if (BitConverter.IsLittleEndian) Array.Reverse(stepBytes);

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(stepBytes);
            var offset = hash[^1] & 0x0F;
            var binary = ((hash[offset] & 0x7F) << 24)
                | ((hash[offset + 1] & 0xFF) << 16)
                | ((hash[offset + 2] & 0xFF) << 8)
                | (hash[offset + 3] & 0xFF);
            var codigo = binary % (int)Math.Pow(10, Digitos);
            return codigo.ToString().PadLeft(Digitos, '0');
        }

        private static string ParaBase32(byte[] bytes)
        {
            var sb = new StringBuilder();
            int buffer = 0, bitsLeft = 0;
            foreach (var b in bytes)
            {
                buffer = (buffer << 8) | b;
                bitsLeft += 8;
                while (bitsLeft >= 5)
                {
                    bitsLeft -= 5;
                    sb.Append(Base32Chars[(buffer >> bitsLeft) & 0x1F]);
                }
            }
            if (bitsLeft > 0) sb.Append(Base32Chars[(buffer << (5 - bitsLeft)) & 0x1F]);
            return sb.ToString();
        }

        private static byte[] DeBase32(string s)
        {
            s = (s ?? string.Empty).Trim().TrimEnd('=').ToUpperInvariant();
            if (s.Length == 0) return Array.Empty<byte>();
            var output = new List<byte>((s.Length * 5 + 7) / 8);
            int buffer = 0, bitsLeft = 0;
            foreach (var c in s)
            {
                var idx = Base32Chars.IndexOf(c);
                if (idx < 0) return Array.Empty<byte>(); // caractere inválido
                buffer = (buffer << 5) | idx;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    output.Add((byte)((buffer >> bitsLeft) & 0xFF));
                }
            }
            return output.ToArray();
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AgendamentoPro.Infrastructure.Services.Pagamento
{
    /// <summary>
    /// Verificação de assinatura HMAC-SHA256 dos webhooks do Mercado Pago.
    /// Mesmo formato é usado pelo gateway transacional e pelo de assinaturas.
    /// Header esperado: "ts=&lt;unixMillis&gt;,v1=&lt;hexHmac&gt;".
    /// </summary>
    internal static class MercadoPagoSignatureVerifier
    {
        public static readonly TimeSpan MaxIdade = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Verifica assinatura do webhook MP. Extrai data.id do payload e valida o HMAC
        /// sobre "id:{dataId};ts:{ts};" com o secret configurado.
        /// </summary>
        public static bool Verificar(string payload, string assinaturaHeader, string secret)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(assinaturaHeader)
                || string.IsNullOrEmpty(secret))
                return false;

            string ts = null, v1 = null;
            foreach (var p in assinaturaHeader.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = p.Trim().Split('=', 2);
                if (kv.Length != 2) continue;
                if (kv[0] == "ts") ts = kv[1];
                else if (kv[0] == "v1") v1 = kv[1];
            }
            if (ts == null || v1 == null) return false;

            // Replay protection
            if (!long.TryParse(ts, out var tsUnixMs)) return false;
            var quandoUtc = DateTimeOffset.FromUnixTimeMilliseconds(tsUnixMs).UtcDateTime;
            if ((DateTime.UtcNow - quandoUtc).Duration() > MaxIdade) return false;

            // data.id é o id que entra no HMAC
            string dataId = null;
            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.TryGetProperty("data", out var data)
                    && data.TryGetProperty("id", out var idEl))
                    dataId = idEl.GetRawText().Trim('"');
            }
            catch { return false; }
            if (dataId == null) return false;

            var payloadAssinatura = $"id:{dataId};ts:{ts};";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadAssinatura));
            var hex = Convert.ToHexString(hash).ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hex), Encoding.UTF8.GetBytes(v1.ToLowerInvariant()));
        }
    }
}

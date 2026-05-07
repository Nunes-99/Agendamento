namespace AgendamentoPro.Core.Entities.Common
{
    /// <summary>
    /// Registro de alteração capturado pelo AuditInterceptor.
    /// Útil para LGPD, troubleshooting e detecção de uso indevido.
    /// </summary>
    public class LogAuditoria
    {
        public int LogId { get; private set; }
        public int? R_TenId { get; private set; }
        public int? R_UsuId { get; private set; }
        public string LogUsuarioEmail { get; private set; }
        public string LogIp { get; private set; }
        public string LogCorrelationId { get; private set; }
        public string LogTabela { get; private set; }
        public string LogChave { get; private set; }
        public string LogAcao { get; private set; } // Insert | Update | Delete
        public string LogValoresAntes { get; private set; }
        public string LogValoresDepois { get; private set; }
        public DateTime LogQuandoUtc { get; private set; }

        protected LogAuditoria() { }

        public LogAuditoria(int? rTenId, int? rUsuId, string usuarioEmail, string ip, string correlationId,
            string tabela, string chave, string acao, string valoresAntes, string valoresDepois)
        {
            R_TenId = rTenId;
            R_UsuId = rUsuId;
            LogUsuarioEmail = usuarioEmail;
            LogIp = ip;
            LogCorrelationId = correlationId;
            LogTabela = tabela;
            LogChave = chave;
            LogAcao = acao;
            LogValoresAntes = valoresAntes;
            LogValoresDepois = valoresDepois;
            LogQuandoUtc = DateTime.UtcNow;
        }
    }
}

namespace AgendamentoPro.Core.Entities.Tenants
{
    /// <summary>
    /// Configurações específicas do tenant (chave/valor) para personalizações flexíveis,
    /// como mensagens, integrações de pagamento e templates de notificação.
    /// </summary>
    public class ConfiguracaoTenant
    {
        public int CfgId { get; private set; }
        public int R_TenId { get; private set; }
        public string CfgChave { get; private set; }
        public string CfgValor { get; private set; }
        public string CfgGrupo { get; private set; }
        public bool CfgSensivel { get; private set; }
        public DateTime CfgAtualizadoEm { get; private set; }

        public Tenant Tenant { get; private set; }

        protected ConfiguracaoTenant() { }

        public ConfiguracaoTenant(int rTenId, string chave, string valor, string grupo, bool sensivel)
        {
            R_TenId = rTenId;
            CfgChave = chave;
            CfgValor = valor;
            CfgGrupo = grupo;
            CfgSensivel = sensivel;
            CfgAtualizadoEm = DateTime.UtcNow;
        }

        public void AlterarValor(string valor)
        {
            CfgValor = valor;
            CfgAtualizadoEm = DateTime.UtcNow;
        }
    }
}

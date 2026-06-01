namespace AgendamentoPro.Core.Enums
{
    /// <summary>
    /// Estados de uma assinatura SaaS (mensalidade do tenant para usar o sistema).
    /// Não confundir com StatusPagamento, que é transacional (cliente final paga pelo serviço agendado).
    /// </summary>
    public enum StatusAssinatura
    {
        /// <summary>Aguardando primeiro pagamento ou em período de teste.</summary>
        Trial = 0,

        /// <summary>Em dia. Acesso total ao sistema.</summary>
        Ativa = 1,

        /// <summary>Cobrança falhou — grace period D+0 a D+7. Acesso total + banners de aviso.</summary>
        Atrasada = 2,

        /// <summary>Inadimplente D+8 a D+30. Acesso somente leitura. Área pública offline.</summary>
        ReadOnly = 3,

        /// <summary>Cancelada pelo dono do tenant (manual). Mantida até fim do ciclo pago.</summary>
        Cancelada = 4,

        /// <summary>Expirada (D+30+ de inadimplência). Tenant soft deleted com 90d de retenção.</summary>
        Expirada = 5
    }
}

using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Clientes
{
    /// <summary>
    /// Saldo de pontos do cliente (programa de fidelidade simples).
    /// Cada agendamento concluído gera N pontos (configurável); cliente troca
    /// pontos por cupom de desconto auto-gerado.
    /// </summary>
    public class PontosFidelidade : ITenantScoped
    {
        public int PtsId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_CliId { get; private set; }
        public int PtsSaldo { get; private set; }
        public DateTime PtsAtualizadoEm { get; private set; }

        public Tenant Tenant { get; private set; }
        public Cliente Cliente { get; private set; }

        protected PontosFidelidade() { }

        public PontosFidelidade(int rTenId, int rCliId)
        {
            if (rTenId <= 0 || rCliId <= 0) throw new DomainException("Tenant e cliente obrigatórios.");
            R_TenId = rTenId;
            R_CliId = rCliId;
            PtsSaldo = 0;
            PtsAtualizadoEm = DateTime.UtcNow;
        }

        public void Creditar(int pontos)
        {
            if (pontos <= 0) return;
            PtsSaldo += pontos;
            PtsAtualizadoEm = DateTime.UtcNow;
        }

        public bool Debitar(int pontos)
        {
            if (pontos <= 0 || pontos > PtsSaldo) return false;
            PtsSaldo -= pontos;
            PtsAtualizadoEm = DateTime.UtcNow;
            return true;
        }
    }
}

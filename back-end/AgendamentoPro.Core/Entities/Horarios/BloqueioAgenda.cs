using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Horarios
{
    public class BloqueioAgenda : ITenantScoped
    {
        public int BloId { get; private set; }
        public int R_TenId { get; private set; }
        public int? R_RecId { get; private set; }
        public DateTime BloDataInicio { get; private set; }
        public DateTime BloDataFim { get; private set; }
        public string BloMotivo { get; private set; }
        public DateTime BloCriadoEm { get; private set; }

        public Tenant Tenant { get; private set; }

        protected BloqueioAgenda() { }

        public BloqueioAgenda(int rTenId, int? rRecId, DateTime inicio, DateTime fim, string motivo)
        {
            R_TenId = rTenId;
            R_RecId = rRecId;
            BloDataInicio = inicio;
            BloDataFim = fim;
            BloMotivo = motivo;
            BloCriadoEm = DateTime.UtcNow;
        }
    }
}

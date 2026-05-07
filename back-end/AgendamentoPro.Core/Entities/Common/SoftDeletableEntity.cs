using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Common
{
    public abstract class SoftDeletableEntity : ISoftDeletable
    {
        public bool Excluido { get; protected set; }
        public DateTime? ExcluidoEm { get; protected set; }

        public virtual void Excluir()
        {
            Excluido = true;
            ExcluidoEm = DateTime.UtcNow;
        }

        public virtual void Restaurar()
        {
            Excluido = false;
            ExcluidoEm = null;
        }
    }
}

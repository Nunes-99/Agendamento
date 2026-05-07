namespace AgendamentoPro.Core.Interfaces.Common
{
    public interface ISoftDeletable
    {
        bool Excluido { get; }
        DateTime? ExcluidoEm { get; }
        void Excluir();
        void Restaurar();
    }
}

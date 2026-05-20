using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Agendamentos
{
    public enum TipoFoto
    {
        Antes = 1,
        Depois = 2,
        Geral = 3
    }

    /// <summary>
    /// Foto vinculada a um agendamento (antes/depois do serviço, ou registro geral).
    /// O conteúdo binário é gravado pelo storage; a entidade só guarda a URL relativa.
    /// </summary>
    public class FotoAgendamento : ITenantScoped
    {
        public int FotId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_AgeId { get; private set; }
        public TipoFoto FotTipo { get; private set; }
        public string FotUrl { get; private set; }
        public string FotNomeOriginal { get; private set; }
        public string FotContentType { get; private set; }
        public long FotTamanhoBytes { get; private set; }
        public DateTime FotCriadoEm { get; private set; }

        public Tenant Tenant { get; private set; }
        public Agendamento Agendamento { get; private set; }

        protected FotoAgendamento() { }

        public FotoAgendamento(int rTenId, int rAgeId, TipoFoto tipo, string url,
            string nomeOriginal, string contentType, long tamanhoBytes)
        {
            if (rTenId <= 0) throw new DomainException("Tenant é obrigatório.");
            if (rAgeId <= 0) throw new DomainException("Agendamento é obrigatório.");
            if (string.IsNullOrWhiteSpace(url)) throw new DomainException("URL da foto é obrigatória.");

            R_TenId = rTenId;
            R_AgeId = rAgeId;
            FotTipo = tipo;
            FotUrl = url;
            FotNomeOriginal = nomeOriginal;
            FotContentType = contentType;
            FotTamanhoBytes = tamanhoBytes;
            FotCriadoEm = DateTime.UtcNow;
        }

        /// <summary>
        /// Atualiza o tamanho gravado no banco após o resize concluir em background.
        /// O upload original pode ter 5 MB; após o resize sobra ~500 KB — sem essa
        /// chamada o banco continuaria reportando o valor antigo.
        /// </summary>
        public void AtualizarTamanho(long bytes)
        {
            if (bytes < 0) throw new DomainException("Tamanho não pode ser negativo.");
            FotTamanhoBytes = bytes;
        }
    }
}

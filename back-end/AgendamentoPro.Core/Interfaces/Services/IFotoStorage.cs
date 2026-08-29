namespace AgendamentoPro.Core.Interfaces.Services
{
    /// <summary>Resultado do upload de uma foto: URL exposta e tamanho real em disco.</summary>
    public readonly record struct FotoSalvaResult(string Url, long TamanhoBytes);

    /// <summary>
    /// Abstrai onde as fotos ficam fisicamente. Implementação default é em disco
    /// (LocalFotoStorage), mas pode ser trocada por S3, Azure Blob, etc.
    /// </summary>
    public interface IFotoStorage
    {
        /// <summary>
        /// Salva o arquivo e retorna a URL relativa (servida estaticamente)
        /// junto com o tamanho real gravado em disco.
        /// </summary>
        Task<FotoSalvaResult> SalvarAsync(int tenantId, int agendamentoId,
            string nomeOriginal, string contentType, Stream conteudo, CancellationToken ct = default);

        /// <summary>
        /// Salva uma imagem da vitrine do tenant (logo/banner/favicon) — fora do
        /// escopo de agendamento, em {tenantId}/vitrine/{tipo}-{guid}.{ext}.
        /// </summary>
        Task<FotoSalvaResult> SalvarVitrineAsync(int tenantId, string tipo,
            string nomeOriginal, string contentType, Stream conteudo, CancellationToken ct = default);

        /// <summary>Remove o arquivo identificado pela URL retornada por SalvarAsync.</summary>
        Task RemoverAsync(string urlRelativa, CancellationToken ct = default);

        /// <summary>
        /// Resolve o caminho físico a partir da URL relativa (usado pelo job
        /// de resize, que precisa ler/sobrescrever o arquivo).
        /// </summary>
        string ResolverCaminho(string urlRelativa);
    }
}

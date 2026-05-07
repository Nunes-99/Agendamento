namespace AgendamentoPro.Core.Interfaces.Services
{
    /// <summary>
    /// Abstrai onde as fotos ficam fisicamente. Implementação default é em disco
    /// (LocalFotoStorage), mas pode ser trocada por S3, Azure Blob, etc.
    /// </summary>
    public interface IFotoStorage
    {
        /// <summary>Salva o arquivo e retorna a URL relativa (servida estaticamente).</summary>
        Task<string> SalvarAsync(int tenantId, int agendamentoId,
            string nomeOriginal, string contentType, Stream conteudo, CancellationToken ct = default);

        /// <summary>Remove o arquivo identificado pela URL retornada por SalvarAsync.</summary>
        Task RemoverAsync(string urlRelativa, CancellationToken ct = default);
    }
}

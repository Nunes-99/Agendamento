namespace AgendamentoPro.Core.Interfaces.Services
{
    /// <summary>Imagem da vitrine após crop/redimensionamento, pronta para o storage.</summary>
    public readonly record struct VitrineImagemProcessada(Stream Conteudo, string ContentType, string Extensao);

    /// <summary>
    /// Enquadra e redimensiona imagens da vitrine no momento do upload, por tipo:
    /// logo cabe em 512×512 (sem crop — logo cortado vira outra marca), banner é
    /// cortado ao centro para proporção de capa (3:1, largura máx. 1920) e favicon
    /// vira PNG quadrado 128×128. Nunca amplia imagem pequena. Também valida que o
    /// conteúdo é de fato uma imagem decodificável — extensão certa com bytes
    /// errados é rejeitada.
    /// </summary>
    public interface IVitrineImagemProcessor
    {
        Task<VitrineImagemProcessada> ProcessarAsync(string tipo, Stream original, CancellationToken ct = default);
    }
}

namespace AgendamentoPro.Application.Interfaces.Assinaturas
{
    /// <summary>
    /// Aplica limites do plano contratado pelo tenant (nº profissionais, unidades, etc).
    /// Use case que vai criar uma entity quotada (Recurso, Unidade, etc) deve chamar
    /// o método correspondente ANTES do CreateAsync para falhar cedo com mensagem clara.
    /// </summary>
    public interface IPlanoLimiteService
    {
        /// <summary>Garante que pode cadastrar mais um profissional/recurso. Lança LimiteDoPlanoException se não.</summary>
        Task GarantirPodeCadastrarProfissionalAsync(int tenantId);
    }
}

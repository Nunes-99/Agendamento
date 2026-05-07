using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Application.ViewModels.Agendamentos;

namespace AgendamentoPro.Application.Interfaces.Agendamentos
{
    public interface IAvaliacaoUseCase
    {
        /// <summary>Cria registro de avaliação pendente quando agendamento é concluído.
        /// Retorna o token público que o cliente usará.</summary>
        Task<Guid> AbrirAsync(int tenantId, int agendamentoId);

        /// <summary>Busca avaliação por token (rota pública /avaliar/{token}).</summary>
        Task<AvaliacaoViewModel> BuscarPorTokenAsync(Guid token);

        /// <summary>Cliente final responde a avaliação via token público.</summary>
        Task<AvaliacaoViewModel> ResponderAsync(Guid token, ResponderAvaliacaoInputModel input);

        /// <summary>Listagem admin paginada.</summary>
        Task<(IEnumerable<AvaliacaoViewModel> Items, int Total)> ListarAsync(int tenantId, int page, int pageSize, bool somenteRespondidas);

        /// <summary>Resumo público (média + últimas N) - usado na home do tenant.</summary>
        Task<ResumoAvaliacoesViewModel> ResumoAsync(int tenantId, int top = 5);

        /// <summary>Admin alterna se a avaliação aparece publicamente.</summary>
        Task AlterarVisibilidadeAsync(int tenantId, int avaliacaoId, bool publica);
    }
}

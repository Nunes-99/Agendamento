using AgendamentoPro.Application.ViewModels.Agendamentos;
using AgendamentoPro.Core.Entities.Agendamentos;

namespace AgendamentoPro.Application.Interfaces.Agendamentos
{
    public interface IFotoAgendamentoUseCase
    {
        Task<FotoAgendamentoViewModel> UploadAsync(int agendamentoId, TipoFoto tipo,
            string nomeOriginal, string contentType, Stream conteudo, CancellationToken ct = default);
        Task<IEnumerable<FotoAgendamentoViewModel>> ListarAsync(int agendamentoId);
        Task RemoverAsync(int fotoId);
    }
}

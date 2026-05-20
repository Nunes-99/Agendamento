using AgendamentoPro.Application.Interfaces.Agendamentos;
using AgendamentoPro.Application.ViewModels.Agendamentos;
using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;

namespace AgendamentoPro.Application.UseCases.Agendamentos
{
    public class FotoAgendamentoUseCase : IFotoAgendamentoUseCase
    {
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IFotoAgendamentoRepository _fotos;
        private readonly IFotoStorage _storage;
        private readonly ITenantContext _tenant;
        private readonly IUnitOfWork _uow;
        private readonly IFotoResizeEnqueuer _resizeEnqueuer;

        public FotoAgendamentoUseCase(IAgendamentoRepository agendamentos,
            IFotoAgendamentoRepository fotos, IFotoStorage storage,
            ITenantContext tenant, IUnitOfWork uow,
            IFotoResizeEnqueuer resizeEnqueuer = null)
        {
            _agendamentos = agendamentos;
            _fotos = fotos;
            _storage = storage;
            _tenant = tenant;
            _uow = uow;
            _resizeEnqueuer = resizeEnqueuer;
        }

        public async Task<FotoAgendamentoViewModel> UploadAsync(int agendamentoId, TipoFoto tipo,
            string nomeOriginal, string contentType, Stream conteudo, CancellationToken ct = default)
        {
            if (!_tenant.IsResolved)
                throw new UnauthorizedAccessException("Tenant não resolvido.");

            var ag = await _agendamentos.GetByIdAsync(agendamentoId, _tenant.TenantId.Value)
                ?? throw new AgendamentoException("Agendamento não encontrado.");

            var salvo = await _storage.SalvarAsync(ag.R_TenId, ag.AgeId, nomeOriginal, contentType, conteudo, ct);

            var foto = new FotoAgendamento(ag.R_TenId, ag.AgeId, tipo, salvo.Url,
                nomeOriginal, contentType, salvo.TamanhoBytes);
            await _fotos.CreateAsync(foto);

            // Resize ocorre em background; o job atualiza FotTamanhoBytes ao terminar
            // pra refletir o tamanho real do arquivo após o resize (que pode ser
            // bem menor que o original).
            _resizeEnqueuer?.Enfileirar(foto.FotId, ag.R_TenId, salvo.Url);

            return ToViewModel(foto);
        }

        public async Task<IEnumerable<FotoAgendamentoViewModel>> ListarAsync(int agendamentoId)
        {
            if (!_tenant.IsResolved)
                throw new UnauthorizedAccessException("Tenant não resolvido.");
            var fotos = await _fotos.GetByAgendamentoAsync(agendamentoId, _tenant.TenantId.Value);
            return fotos.Select(ToViewModel);
        }

        public async Task RemoverAsync(int fotoId)
        {
            if (!_tenant.IsResolved)
                throw new UnauthorizedAccessException("Tenant não resolvido.");
            var foto = await _fotos.GetByIdAsync(fotoId, _tenant.TenantId.Value)
                ?? throw new AgendamentoException("Foto não encontrada.");
            await _storage.RemoverAsync(foto.FotUrl);
            await _fotos.DeleteAsync(fotoId, _tenant.TenantId.Value);
            await _uow.SaveChangesAsync();
        }

        private static FotoAgendamentoViewModel ToViewModel(FotoAgendamento f) => new()
        {
            Id = f.FotId,
            AgendamentoId = f.R_AgeId,
            Tipo = f.FotTipo,
            Url = f.FotUrl,
            ContentType = f.FotContentType,
            TamanhoBytes = f.FotTamanhoBytes,
            CriadoEm = f.FotCriadoEm
        };
    }

    /// <summary>
    /// Abstração pra enfileirar o resize sem acoplar a Application ao Hangfire.
    /// </summary>
    public interface IFotoResizeEnqueuer
    {
        void Enfileirar(int fotoId, int tenantId, string urlRelativa);
    }
}

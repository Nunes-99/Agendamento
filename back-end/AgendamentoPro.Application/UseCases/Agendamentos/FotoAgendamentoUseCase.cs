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

        public FotoAgendamentoUseCase(IAgendamentoRepository agendamentos,
            IFotoAgendamentoRepository fotos, IFotoStorage storage,
            ITenantContext tenant, IUnitOfWork uow)
        {
            _agendamentos = agendamentos;
            _fotos = fotos;
            _storage = storage;
            _tenant = tenant;
            _uow = uow;
        }

        public async Task<FotoAgendamentoViewModel> UploadAsync(int agendamentoId, TipoFoto tipo,
            string nomeOriginal, string contentType, Stream conteudo, CancellationToken ct = default)
        {
            if (!_tenant.IsResolved)
                throw new UnauthorizedAccessException("Tenant não resolvido.");

            var ag = await _agendamentos.GetByIdAsync(agendamentoId, _tenant.TenantId.Value)
                ?? throw new AgendamentoException("Agendamento não encontrado.");

            var url = await _storage.SalvarAsync(ag.R_TenId, ag.AgeId, nomeOriginal, contentType, conteudo, ct);

            // contentLength obtido pela posição do stream após cópia para o storage,
            // mas como já consumimos, recuperamos via FileInfo? Para simplicidade armazena 0;
            // o storage pode ser estendido para retornar tamanho. Aqui usa Stream.Length se disponível.
            long tamanho = 0;
            try { if (conteudo.CanSeek) tamanho = conteudo.Length; } catch { }

            var foto = new FotoAgendamento(ag.R_TenId, ag.AgeId, tipo, url, nomeOriginal, contentType, tamanho);
            await _fotos.CreateAsync(foto);

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
}

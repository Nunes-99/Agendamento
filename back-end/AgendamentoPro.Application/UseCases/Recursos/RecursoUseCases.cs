using AgendamentoPro.Application.InputModels.Recursos;
using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Application.Interfaces.Recursos;
using AgendamentoPro.Application.ViewModels.Recursos;
using AgendamentoPro.Core.Entities.Recursos;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Recursos
{
    internal static class RecursoMapper
    {
        public static RecursoViewModel Map(Recurso r) => new()
        {
            Id = r.RecId,
            TenantId = r.R_TenId,
            Nome = r.RecNome,
            Descricao = r.RecDescricao,
            Tipo = r.RecTipo,
            ImagemUrl = r.RecImagemUrl,
            Ordem = r.RecOrdem,
            Ativo = r.RecAtivo
        };
    }

    public class CadastrarRecursoUseCase : ICadastrarRecursoUseCase
    {
        private readonly IRecursoRepository _recursos;
        private readonly IUnitOfWork _uow;
        private readonly IPlanoLimiteService _limites;
        public CadastrarRecursoUseCase(IRecursoRepository r, IUnitOfWork u, IPlanoLimiteService limites)
        {
            _recursos = r; _uow = u; _limites = limites;
        }
        public async Task<RecursoViewModel> ExecuteAsync(int tenantId, RecursoInputModel input)
        {
            await _limites.GarantirPodeCadastrarProfissionalAsync(tenantId);
            var rec = new Recurso(tenantId, input.Nome, input.Descricao, input.Tipo, input.ImagemUrl, input.Ordem);
            if (!input.Ativo) rec.Inativar();
            await _recursos.CreateAsync(rec);
            await _uow.SaveChangesAsync();
            return RecursoMapper.Map(rec);
        }
    }

    public class AtualizarRecursoUseCase : IAtualizarRecursoUseCase
    {
        private readonly IRecursoRepository _recursos;
        private readonly IUnitOfWork _uow;
        public AtualizarRecursoUseCase(IRecursoRepository r, IUnitOfWork u) { _recursos = r; _uow = u; }
        public async Task<RecursoViewModel> ExecuteAsync(int tenantId, int id, RecursoInputModel input)
        {
            var rec = await _recursos.GetByIdAsync(id, tenantId) ?? throw new DomainException("Recurso não encontrado.");
            rec.Atualizar(input.Nome, input.Descricao, input.Tipo, input.ImagemUrl, input.Ordem);
            if (input.Ativo) rec.Ativar(); else rec.Inativar();
            await _recursos.UpdateAsync(rec);
            await _uow.SaveChangesAsync();
            return RecursoMapper.Map(rec);
        }
    }

    public class ConsultarRecursoUseCase : IConsultarRecursoUseCase
    {
        private readonly IRecursoRepository _recursos;
        public ConsultarRecursoUseCase(IRecursoRepository r) { _recursos = r; }
        public async Task<RecursoViewModel> PorIdAsync(int tenantId, int id)
        {
            var r = await _recursos.GetByIdAsync(id, tenantId);
            return r == null ? null : RecursoMapper.Map(r);
        }
        public async Task<IEnumerable<RecursoViewModel>> ListarAsync(int tenantId, bool somenteAtivos)
        {
            var lista = await _recursos.GetByTenantAsync(tenantId, somenteAtivos);
            return lista.Select(RecursoMapper.Map);
        }
    }

    public class InativarRecursoUseCase : IInativarRecursoUseCase
    {
        private readonly IRecursoRepository _recursos;
        private readonly IUnitOfWork _uow;
        public InativarRecursoUseCase(IRecursoRepository r, IUnitOfWork u) { _recursos = r; _uow = u; }
        public async Task ExecuteAsync(int tenantId, int id)
        {
            await _recursos.DeleteAsync(id, tenantId);
            await _uow.SaveChangesAsync();
        }
    }
}

using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Application.InputModels.Assinaturas;
using AgendamentoPro.Application.Mappers;
using AgendamentoPro.Application.ViewModels.Assinaturas;
using AgendamentoPro.Core.Entities.Assinaturas;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Assinaturas
{
    public class ListarTodosPlanosUseCase : IListarTodosPlanosUseCase
    {
        private readonly IPlanoRepository _planos;
        public ListarTodosPlanosUseCase(IPlanoRepository planos) { _planos = planos; }

        public async Task<IEnumerable<PlanoViewModel>> ExecuteAsync()
            => (await _planos.ListarTodosAsync()).Select(AssinaturaMapper.ToViewModel);
    }

    public class CriarPlanoUseCase : ICriarPlanoUseCase
    {
        private readonly IPlanoRepository _planos;
        private readonly IUnitOfWork _uow;
        public CriarPlanoUseCase(IPlanoRepository planos, IUnitOfWork uow) { _planos = planos; _uow = uow; }

        public async Task<PlanoViewModel> ExecuteAsync(PlanoCatalogoInputModel input)
        {
            if (input == null) throw new DomainException("Dados do plano ausentes.");
            var plano = new Plano(input.Nome, input.Descricao, input.Preco,
                input.LimiteUnidades, input.LimiteProfissionais, input.LimiteAgendamentosMes,
                input.Publico, input.Ordem);
            await _planos.CreateAsync(plano);
            await _uow.SaveChangesAsync();
            return AssinaturaMapper.ToViewModel(plano);
        }
    }

    public class AtualizarPlanoUseCase : IAtualizarPlanoUseCase
    {
        private readonly IPlanoRepository _planos;
        private readonly IUnitOfWork _uow;
        public AtualizarPlanoUseCase(IPlanoRepository planos, IUnitOfWork uow) { _planos = planos; _uow = uow; }

        public async Task<PlanoViewModel> ExecuteAsync(int planoId, PlanoCatalogoInputModel input)
        {
            if (input == null) throw new DomainException("Dados do plano ausentes.");
            var plano = await _planos.GetByIdAsync(planoId)
                ?? throw new DomainException("Plano não encontrado.");

            plano.Atualizar(input.Nome, input.Descricao, input.Preco,
                input.LimiteUnidades, input.LimiteProfissionais, input.LimiteAgendamentosMes, input.Ordem);
            if (input.Publico) plano.TornarPublico(); else plano.TornarPrivado();

            await _planos.UpdateAsync(plano);
            await _uow.SaveChangesAsync();
            return AssinaturaMapper.ToViewModel(plano);
        }
    }

    public class AlternarStatusPlanoUseCase : IAlternarStatusPlanoUseCase
    {
        private readonly IPlanoRepository _planos;
        private readonly IUnitOfWork _uow;
        public AlternarStatusPlanoUseCase(IPlanoRepository planos, IUnitOfWork uow) { _planos = planos; _uow = uow; }

        public async Task<PlanoViewModel> ExecuteAsync(int planoId, bool ativo)
        {
            var plano = await _planos.GetByIdAsync(planoId)
                ?? throw new DomainException("Plano não encontrado.");
            if (ativo) plano.Ativar(); else plano.Inativar();
            await _planos.UpdateAsync(plano);
            await _uow.SaveChangesAsync();
            return AssinaturaMapper.ToViewModel(plano);
        }
    }
}

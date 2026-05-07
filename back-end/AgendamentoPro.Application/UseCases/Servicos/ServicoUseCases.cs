using AgendamentoPro.Application.InputModels.Servicos;
using AgendamentoPro.Application.Interfaces.Servicos;
using AgendamentoPro.Application.ViewModels.Servicos;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Servicos
{
    internal static class ServicoMapper
    {
        public static ServicoViewModel Map(Servico s) => new()
        {
            Id = s.SerId,
            TenantId = s.R_TenId,
            Nome = s.SerNome,
            Descricao = s.SerDescricao,
            Preco = s.SerPreco,
            DuracaoMinutos = s.SerDuracaoMinutos,
            ImagemUrl = s.SerImagemUrl,
            Categoria = s.SerCategoria,
            Ordem = s.SerOrdem,
            Ativo = s.SerAtivo
        };
    }

    public class CadastrarServicoUseCase : ICadastrarServicoUseCase
    {
        private readonly IServicoRepository _servicos;
        private readonly IUnitOfWork _uow;

        public CadastrarServicoUseCase(IServicoRepository servicos, IUnitOfWork uow)
        {
            _servicos = servicos;
            _uow = uow;
        }

        public async Task<ServicoViewModel> ExecuteAsync(int tenantId, ServicoInputModel input)
        {
            var servico = new Servico(tenantId, input.Nome, input.Descricao, input.Preco,
                input.DuracaoMinutos, input.ImagemUrl, input.Categoria, input.Ordem);
            if (!input.Ativo) servico.Inativar();
            await _servicos.CreateAsync(servico);
            await _uow.SaveChangesAsync();
            return ServicoMapper.Map(servico);
        }
    }

    public class AtualizarServicoUseCase : IAtualizarServicoUseCase
    {
        private readonly IServicoRepository _servicos;
        private readonly IUnitOfWork _uow;

        public AtualizarServicoUseCase(IServicoRepository servicos, IUnitOfWork uow)
        {
            _servicos = servicos;
            _uow = uow;
        }

        public async Task<ServicoViewModel> ExecuteAsync(int tenantId, int id, ServicoInputModel input)
        {
            var servico = await _servicos.GetByIdAsync(id, tenantId) ?? throw new ServicoException("Serviço não encontrado.");
            servico.Atualizar(input.Nome, input.Descricao, input.Preco,
                input.DuracaoMinutos, input.ImagemUrl, input.Categoria, input.Ordem);
            if (input.Ativo) servico.Ativar(); else servico.Inativar();
            await _servicos.UpdateAsync(servico);
            await _uow.SaveChangesAsync();
            return ServicoMapper.Map(servico);
        }
    }

    public class ConsultarServicoUseCase : IConsultarServicoUseCase
    {
        private readonly IServicoRepository _servicos;
        public ConsultarServicoUseCase(IServicoRepository servicos) { _servicos = servicos; }

        public async Task<ServicoViewModel> PorIdAsync(int tenantId, int id)
        {
            var s = await _servicos.GetByIdAsync(id, tenantId);
            return s == null ? null : ServicoMapper.Map(s);
        }

        public async Task<IEnumerable<ServicoViewModel>> ListarAsync(int tenantId, bool somenteAtivos)
        {
            var lista = await _servicos.GetByTenantAsync(tenantId, somenteAtivos);
            return lista.Select(ServicoMapper.Map);
        }
    }

    public class InativarServicoUseCase : IInativarServicoUseCase
    {
        private readonly IServicoRepository _servicos;
        private readonly IUnitOfWork _uow;
        public InativarServicoUseCase(IServicoRepository servicos, IUnitOfWork uow)
        {
            _servicos = servicos;
            _uow = uow;
        }
        public async Task ExecuteAsync(int tenantId, int id)
        {
            await _servicos.DeleteAsync(id, tenantId);
            await _uow.SaveChangesAsync();
        }
    }
}

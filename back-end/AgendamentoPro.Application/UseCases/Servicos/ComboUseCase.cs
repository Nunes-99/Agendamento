using AgendamentoPro.Application.InputModels.Servicos;
using AgendamentoPro.Application.Interfaces.Servicos;
using AgendamentoPro.Application.ViewModels.Servicos;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Servicos
{
    public class ComboUseCase : IComboUseCase
    {
        private readonly IComboRepository _combos;
        private readonly IServicoRepository _servicos;
        private readonly IUnitOfWork _uow;

        public ComboUseCase(IComboRepository c, IServicoRepository s, IUnitOfWork u)
        {
            _combos = c;
            _servicos = s;
            _uow = u;
        }

        public async Task<ComboViewModel> CriarAsync(int tenantId, ComboInputModel input)
        {
            await GarantirServicosValidos(tenantId, input.ServicoIds);

            var combo = new Combo(tenantId, input.Nome, input.Descricao, input.ImagemUrl,
                input.PrecoPromocional, input.Ordem);
            combo.DefinirServicos(input.ServicoIds);
            await _combos.CreateAsync(combo);
            await _uow.SaveChangesAsync();

            return await ObterAsync(tenantId, combo.ComId);
        }

        public async Task<ComboViewModel> AtualizarAsync(int tenantId, int id, ComboInputModel input)
        {
            var combo = await _combos.GetByIdAsync(id, tenantId)
                ?? throw new ServicoException("Combo não encontrado.");
            await GarantirServicosValidos(tenantId, input.ServicoIds);
            combo.Atualizar(input.Nome, input.Descricao, input.ImagemUrl,
                input.PrecoPromocional, input.Ordem, input.Ativo);
            combo.DefinirServicos(input.ServicoIds);
            await _combos.UpdateAsync(combo);
            await _uow.SaveChangesAsync();
            return await ObterAsync(tenantId, id);
        }

        public async Task RemoverAsync(int tenantId, int id)
        {
            await _combos.DeleteAsync(id, tenantId);
            await _uow.SaveChangesAsync();
        }

        public async Task<ComboViewModel> ObterAsync(int tenantId, int id)
        {
            var c = await _combos.GetByIdAsync(id, tenantId);
            return c == null ? null : Map(c);
        }

        public async Task<IEnumerable<ComboViewModel>> ListarAsync(int tenantId, bool somenteAtivos)
        {
            var lista = await _combos.GetByTenantAsync(tenantId, somenteAtivos);
            return lista.Select(Map);
        }

        private async Task GarantirServicosValidos(int tenantId, IEnumerable<int> ids)
        {
            foreach (var sid in ids)
            {
                var s = await _servicos.GetByIdAsync(sid, tenantId);
                if (s == null) throw new ServicoException($"Serviço {sid} não pertence ao tenant ou não existe.");
            }
        }

        private static ComboViewModel Map(Combo c) => new()
        {
            Id = c.ComId,
            Nome = c.ComNome,
            Descricao = c.ComDescricao,
            ImagemUrl = c.ComImagemUrl,
            PrecoOriginal = c.Servicos?.Where(s => s.Servico != null).Sum(s => s.Servico.SerPreco) ?? 0m,
            PrecoPromocional = c.ComPrecoPromocional,
            Ordem = c.ComOrdem,
            Ativo = c.ComAtivo,
            Servicos = c.Servicos?.Where(s => s.Servico != null).Select(s => new ComboServicoViewModel
            {
                ServicoId = s.Servico.SerId,
                Nome = s.Servico.SerNome,
                Preco = s.Servico.SerPreco,
                DuracaoMinutos = s.Servico.SerDuracaoMinutos
            }).ToList() ?? new()
        };
    }
}

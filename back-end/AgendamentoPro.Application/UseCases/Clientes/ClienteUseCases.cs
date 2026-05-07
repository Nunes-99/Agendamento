using AgendamentoPro.Application.InputModels.Clientes;
using AgendamentoPro.Application.Interfaces.Clientes;
using AgendamentoPro.Application.ViewModels.Clientes;
using AgendamentoPro.Application.ViewModels.Common;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Clientes
{
    internal static class ClienteMapper
    {
        public static ClienteViewModel Map(Cliente c) => new()
        {
            Id = c.CliId,
            Nome = c.CliNome,
            Email = c.CliEmail,
            Telefone = c.CliTelefone,
            WhatsApp = c.CliWhatsApp,
            Cpf = c.CliCpf,
            Observacao = c.CliObservacao
        };
    }

    public class CadastrarClienteUseCase : ICadastrarClienteUseCase
    {
        private readonly IClienteRepository _clientes;
        private readonly IUnitOfWork _uow;
        public CadastrarClienteUseCase(IClienteRepository c, IUnitOfWork u) { _clientes = c; _uow = u; }
        public async Task<ClienteViewModel> ExecuteAsync(int tenantId, ClienteInputModel input)
        {
            var cliente = new Cliente(tenantId, input.Nome, input.Email, input.Telefone,
                input.WhatsApp, input.Cpf, input.Observacao);
            await _clientes.CreateAsync(cliente);
            await _uow.SaveChangesAsync();
            return ClienteMapper.Map(cliente);
        }
    }

    public class AtualizarClienteUseCase : IAtualizarClienteUseCase
    {
        private readonly IClienteRepository _clientes;
        private readonly IUnitOfWork _uow;
        public AtualizarClienteUseCase(IClienteRepository c, IUnitOfWork u) { _clientes = c; _uow = u; }
        public async Task<ClienteViewModel> ExecuteAsync(int tenantId, int id, ClienteInputModel input)
        {
            var cliente = await _clientes.GetByIdAsync(id, tenantId) ?? throw new ClienteException("Cliente não encontrado.");
            cliente.Atualizar(input.Nome, input.Email, input.Telefone, input.WhatsApp, input.Cpf, input.Observacao);
            await _clientes.UpdateAsync(cliente);
            await _uow.SaveChangesAsync();
            return ClienteMapper.Map(cliente);
        }
    }

    public class ConsultarClienteUseCase : IConsultarClienteUseCase
    {
        private readonly IClienteRepository _clientes;
        public ConsultarClienteUseCase(IClienteRepository c) { _clientes = c; }
        public async Task<ClienteViewModel> PorIdAsync(int tenantId, int id)
        {
            var c = await _clientes.GetByIdAsync(id, tenantId);
            return c == null ? null : ClienteMapper.Map(c);
        }
        public async Task<PaginadoViewModel<ClienteViewModel>> ListarPaginadoAsync(int tenantId, int page, int pageSize, string busca)
        {
            var (items, total) = await _clientes.GetPagedAsync(tenantId, page, pageSize, busca);
            return new PaginadoViewModel<ClienteViewModel>
            {
                Items = items.Select(ClienteMapper.Map),
                Total = total,
                Pagina = page,
                TamanhoPagina = pageSize
            };
        }
    }
}

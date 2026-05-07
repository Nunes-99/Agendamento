using AgendamentoPro.Application.Interfaces.Lgpd;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Application.UseCases.Lgpd
{
    public class LgpdUseCase : ILgpdUseCase
    {
        private readonly IClienteRepository _clientes;
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IAvaliacaoRepository _avaliacoes;
        private readonly IFotoAgendamentoRepository _fotos;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<LgpdUseCase> _logger;

        public LgpdUseCase(IClienteRepository clientes, IAgendamentoRepository agendamentos,
            IAvaliacaoRepository avaliacoes, IFotoAgendamentoRepository fotos,
            IUnitOfWork uow, ILogger<LgpdUseCase> logger)
        {
            _clientes = clientes; _agendamentos = agendamentos;
            _avaliacoes = avaliacoes; _fotos = fotos;
            _uow = uow; _logger = logger;
        }

        public async Task<object> ExportarDadosClienteAsync(int tenantId, int clienteId)
        {
            var c = await _clientes.GetByIdAsync(clienteId, tenantId)
                ?? throw new ClienteException("Cliente não encontrado.");

            var agendamentos = (await _agendamentos.GetPorClienteAsync(tenantId, clienteId)).ToList();
            var fotos = new List<object>();
            foreach (var ag in agendamentos)
            {
                var listaFotos = await _fotos.GetByAgendamentoAsync(ag.AgeId, tenantId);
                fotos.AddRange(listaFotos.Select(f => new { agendamentoId = f.R_AgeId, url = f.FotUrl, tipo = f.FotTipo, criadoEm = f.FotCriadoEm }));
            }

            return new
            {
                exportadoEm = DateTime.UtcNow,
                cliente = new
                {
                    id = c.CliId,
                    nome = c.CliNome,
                    email = c.CliEmail,
                    telefone = c.CliTelefone,
                    whatsApp = c.CliWhatsApp,
                    cpf = c.CliCpf,
                    observacao = c.CliObservacao,
                    criadoEm = c.CliCriadoEm
                },
                agendamentos = agendamentos.Select(a => new
                {
                    id = a.AgeId,
                    data = a.AgeData,
                    horaInicio = a.AgeHoraInicio,
                    horaFim = a.AgeHoraFim,
                    servico = a.Servico?.SerNome,
                    valorTotal = a.AgeValorTotal,
                    status = a.AgeStatus.ToString(),
                    observacao = a.AgeObservacao,
                    criadoEm = a.AgeCriadoEm
                }),
                fotos
            };
        }

        public async Task AnonimizarClienteAsync(int tenantId, int clienteId)
        {
            var c = await _clientes.GetByIdAsync(clienteId, tenantId)
                ?? throw new ClienteException("Cliente não encontrado.");

            // Mantém o registro pra integridade dos agendamentos passados,
            // mas remove qualquer identificação pessoal.
            c.Atualizar(
                nome: $"Cliente removido #{c.CliId}",
                email: null,
                telefone: null,
                whatsapp: null,
                cpf: null,
                observacao: null);
            await _clientes.UpdateAsync(c);
            await _uow.SaveChangesAsync();

            _logger.LogWarning("LGPD: cliente {ClienteId} do tenant {TenantId} foi anonimizado.",
                clienteId, tenantId);
        }

        public async Task<int> AnonimizarInativosAsync(int tenantId, int inativoHaMeses)
        {
            // Critério: cliente sem agendamento nos últimos N meses E sem agendamento
            // futuro. Implementação simples — pega todos os clientes do tenant, checa.
            // Para volumes grandes, vale otimizar com query agregada.
            var (clientes, _) = await _clientes.GetPagedAsync(tenantId, 1, int.MaxValue, null);
            var limite = DateTime.UtcNow.AddMonths(-inativoHaMeses);
            var anonimizados = 0;

            foreach (var c in clientes)
            {
                if (c.CliNome.StartsWith("Cliente removido")) continue; // já anonimizado
                var ags = await _agendamentos.GetPorClienteAsync(tenantId, c.CliId);
                if (ags.All(a => a.AgeData < limite || a.AgeStatus == Core.Enums.StatusAgendamento.Cancelado))
                {
                    await AnonimizarClienteAsync(tenantId, c.CliId);
                    anonimizados++;
                }
            }
            return anonimizados;
        }
    }
}

using AgendamentoPro.Application.Interfaces.Lgpd;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Application.UseCases.Lgpd
{
    public class LgpdUseCase : ILgpdUseCase
    {
        private readonly IClienteRepository _clientes;
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IAvaliacaoRepository _avaliacoes;
        private readonly IFotoAgendamentoRepository _fotos;
        private readonly IFotoStorage _storage;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<LgpdUseCase> _logger;

        public LgpdUseCase(IClienteRepository clientes, IAgendamentoRepository agendamentos,
            IAvaliacaoRepository avaliacoes, IFotoAgendamentoRepository fotos,
            IFotoStorage storage, IUnitOfWork uow, ILogger<LgpdUseCase> logger)
        {
            _clientes = clientes; _agendamentos = agendamentos;
            _avaliacoes = avaliacoes; _fotos = fotos; _storage = storage;
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

            // LGPD direito ao esquecimento: fotos antes/depois podem conter o rosto
            // do cliente. Antes de anonimizar os campos textuais, deletar fisicamente
            // as fotos de TODOS os agendamentos do cliente (storage + DB).
            var agendamentosDoCliente = await _agendamentos.GetPorClienteAsync(tenantId, clienteId);
            var fotosRemovidas = 0;
            foreach (var ag in agendamentosDoCliente)
            {
                var fotos = await _fotos.GetByAgendamentoAsync(ag.AgeId, tenantId);
                foreach (var f in fotos)
                {
                    try { await _storage.RemoverAsync(f.FotUrl); }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "LGPD: falha ao remover arquivo da foto {FotoId} (continuando)", f.FotId);
                    }
                    await _fotos.DeleteAsync(f.FotId, tenantId);
                    fotosRemovidas++;
                }
            }

            // Mantém o registro do Cliente pra integridade dos agendamentos passados,
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

            _logger.LogWarning("LGPD: cliente {ClienteId} do tenant {TenantId} anonimizado ({Fotos} fotos removidas).",
                clienteId, tenantId, fotosRemovidas);
        }

        public async Task<int> AnonimizarInativosAsync(int tenantId, int inativoHaMeses)
        {
            // Single query agregada: já vem só os IDs dos elegíveis. O loop apenas
            // anonimiza cada um. Anteriormente isto era N+1 (uma query por cliente
            // pra checar agendamentos), o que estourava em tenant com 1000+ clientes.
            var corte = DateTime.UtcNow.AddMonths(-inativoHaMeses);
            var ids = (await _clientes.GetIdsInativosAsync(tenantId, corte)).ToList();
            foreach (var id in ids)
            {
                await AnonimizarClienteAsync(tenantId, id);
            }
            var anonimizados = ids.Count;
            return anonimizados;
        }
    }
}

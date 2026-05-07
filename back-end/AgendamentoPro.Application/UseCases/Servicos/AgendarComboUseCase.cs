using AgendamentoPro.Application.Interfaces.Servicos;
using AgendamentoPro.Application.InputModels.Servicos;
using AgendamentoPro.Application.UseCases.Agendamentos;
using AgendamentoPro.Application.ViewModels.Agendamentos;
using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Pagamentos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;

namespace AgendamentoPro.Application.UseCases.Servicos
{
    public class AgendarComboUseCase : IAgendarComboUseCase
    {
        private readonly IComboRepository _combos;
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IServicoRepository _servicos;
        private readonly IRecursoRepository _recursos;
        private readonly IClienteRepository _clientes;
        private readonly ITenantRepository _tenants;
        private readonly IPagamentoRepository _pagamentos;
        private readonly IDisponibilidadeService _disponibilidade;
        private readonly IEnumerable<IGatewayPagamento> _gateways;
        private readonly IUnitOfWork _uow;

        public AgendarComboUseCase(IComboRepository combos, IAgendamentoRepository agendamentos,
            IServicoRepository servicos, IRecursoRepository recursos, IClienteRepository clientes,
            ITenantRepository tenants, IPagamentoRepository pagamentos,
            IDisponibilidadeService disponibilidade, IEnumerable<IGatewayPagamento> gateways, IUnitOfWork uow)
        {
            _combos = combos; _agendamentos = agendamentos; _servicos = servicos;
            _recursos = recursos; _clientes = clientes; _tenants = tenants;
            _pagamentos = pagamentos; _disponibilidade = disponibilidade;
            _gateways = gateways; _uow = uow;
        }

        public async Task<AgendarComboResultViewModel> ExecuteAsync(int tenantId, int comboId, AgendarComboInputModel input)
        {
            var tenant = await _tenants.GetByIdAsync(tenantId)
                ?? throw new TenantException("Estabelecimento não encontrado.");
            if (!tenant.TenAtivo) throw new TenantException("Estabelecimento inativo.");

            var combo = await _combos.GetByIdAsync(comboId, tenantId)
                ?? throw new ServicoException("Combo não encontrado.");
            if (!combo.ComAtivo) throw new ServicoException("Combo indisponível.");
            if (combo.Servicos.Count == 0) throw new ServicoException("Combo sem serviços vinculados.");

            // Carregar serviços (com duração) na ordem do combo
            var servicosDoCombo = combo.Servicos
                .Where(cs => cs.Servico != null && cs.Servico.SerAtivo)
                .Select(cs => cs.Servico!)
                .ToList();
            if (servicosDoCombo.Count != combo.Servicos.Count)
                throw new ServicoException("Algum serviço do combo está inativo. Tente novamente mais tarde.");

            // Antecedência mínima (do primeiro slot)
            var dataHoraInicio = input.Data.Date.Add(input.HoraInicio);
            if (dataHoraInicio < DateTime.Now.AddHours(tenant.TenAntecedenciaMinHoras))
                throw new AgendamentoException($"O agendamento exige antecedência mínima de {tenant.TenAntecedenciaMinHoras}h.");

            // Resolver recurso: se informado usa ele; senão usa o primeiro recurso ativo do tenant.
            // Importante: combo precisa de UM recurso só (todos os atendimentos seguem em sequência no mesmo box).
            int recursoId;
            if (input.RecursoId.HasValue)
            {
                var rec = await _recursos.GetByIdAsync(input.RecursoId.Value, tenantId)
                    ?? throw new DomainException("Recurso não encontrado.");
                if (!rec.RecAtivo) throw new DomainException("Recurso indisponível.");
                recursoId = rec.RecId;
            }
            else
            {
                var lista = (await _recursos.GetByTenantAsync(tenantId, true)).ToList();
                if (lista.Count == 0) throw new DomainException("Sem recursos disponíveis no tenant.");
                recursoId = lista[0].RecId;
            }

            await _uow.BeginTransactionAsync();
            try
            {
                // Cliente: tenta achar; senão cria
                Cliente cliente = null;
                if (!string.IsNullOrWhiteSpace(input.Cliente.Telefone))
                    cliente = await _clientes.GetByTelefoneAsync(tenantId, input.Cliente.Telefone);
                if (cliente == null && !string.IsNullOrWhiteSpace(input.Cliente.Email))
                    cliente = await _clientes.GetByEmailAsync(tenantId, input.Cliente.Email);
                if (cliente == null)
                {
                    cliente = new Cliente(tenantId, input.Cliente.Nome, input.Cliente.Email,
                        input.Cliente.Telefone, input.Cliente.WhatsApp, input.Cliente.Cpf);
                    await _clientes.CreateAsync(cliente);
                }

                // Cria N agendamentos contíguos no mesmo recurso. Buffer aplicado entre o primeiro e
                // qualquer agendamento existente externo ao combo, mas NÃO entre os agendamentos
                // internos do combo (são sequenciais, sem intervalo).
                var buffer = TimeSpan.FromMinutes(tenant.TenBufferMinutos);
                var grupoComboId = Guid.NewGuid();
                var horaCorrente = input.HoraInicio;
                var agendamentosCriados = new List<Agendamento>();

                // Soma dos preços individuais dos serviços (para guardar como AgeValorTotal de cada)
                // mas o pagamento agregado usa combo.ComPrecoPromocional.
                foreach (var servico in servicosDoCombo)
                {
                    var horaFim = horaCorrente.Add(TimeSpan.FromMinutes(servico.SerDuracaoMinutos));

                    // Conflito: aplica buffer no início do primeiro e no fim do último apenas
                    // (entre os internos do combo, sem buffer).
                    var ehPrimeiro = agendamentosCriados.Count == 0;
                    var ehUltimo = agendamentosCriados.Count == servicosDoCombo.Count - 1;
                    var inicioCheck = ehPrimeiro ? horaCorrente.Subtract(buffer) : horaCorrente;
                    var fimCheck = ehUltimo ? horaFim.Add(buffer) : horaFim;

                    if (await _agendamentos.ExisteConflitoAsync(tenantId, recursoId, input.Data.Date,
                            inicioCheck, fimCheck))
                    {
                        throw new AgendamentoException(
                            $"Horário indisponível para o serviço \"{servico.SerNome}\" às {horaCorrente:hh\\:mm}. Tente outro horário.");
                    }

                    var ag = new Agendamento(tenantId, cliente.CliId, servico.SerId, recursoId,
                        input.Data, horaCorrente, horaFim,
                        valorTotal: servico.SerPreco,
                        percentualEntrada: tenant.TenPercentualEntrada,
                        observacao: input.Observacao,
                        grupoComboId: grupoComboId);
                    await _agendamentos.CreateAsync(ag);
                    agendamentosCriados.Add(ag);

                    horaCorrente = horaFim;
                }

                // Pagamento agregado: percentual de entrada sobre o preço promocional do combo,
                // vinculado ao PRIMEIRO agendamento do grupo. Webhook depois confirma todos.
                var primeiro = agendamentosCriados[0];
                var valorEntrada = Math.Round(combo.ComPrecoPromocional * tenant.TenPercentualEntrada / 100m, 2);

                Pagamento pagamento = null;
                if (input.FormaPagamento != FormaPagamento.Dinheiro)
                {
                    var gateway = _gateways.FirstOrDefault()
                        ?? throw new DomainException("Nenhum gateway de pagamento configurado.");
                    var cobranca = await gateway.CriarCobrancaAsync(tenantId, primeiro.AgeId,
                        valorEntrada, input.FormaPagamento,
                        $"Sinal Combo - {combo.ComNome}", 15);

                    pagamento = new Pagamento(tenantId, primeiro.AgeId, input.FormaPagamento,
                        valorEntrada, gateway.Nome, cobranca.Expiracao);
                    pagamento.DefinirDadosGateway(cobranca.GatewayId, cobranca.QrCode,
                        cobranca.LinkPagamento, cobranca.PayloadBruto);
                    await _pagamentos.CreateAsync(pagamento);
                }
                else
                {
                    foreach (var ag in agendamentosCriados)
                    {
                        ag.ConfirmarPagamento();
                        await _agendamentos.UpdateAsync(ag);
                    }
                }

                await _uow.CommitAsync();

                return new AgendarComboResultViewModel
                {
                    GrupoComboId = grupoComboId,
                    Agendamentos = agendamentosCriados.Select(AgendamentoMapper.Map).ToList(),
                    Pagamento = pagamento == null ? null : new PagamentoViewModel
                    {
                        Id = pagamento.PagId,
                        Forma = pagamento.PagForma,
                        Status = pagamento.PagStatus,
                        Valor = pagamento.PagValor,
                        QrCode = pagamento.PagQrCode,
                        LinkPagamento = pagamento.PagLinkPagamento,
                        Expiracao = pagamento.PagExpiracao
                    }
                };
            }
            catch (ConcorrenciaException)
            {
                await _uow.RollbackAsync();
                throw new AgendamentoException("Algum horário do combo já está reservado. Tente novamente.");
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
    }
}

using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Application.Interfaces.Agendamentos;
using AgendamentoPro.Application.ViewModels.Agendamentos;
using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Pagamentos;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;

namespace AgendamentoPro.Application.UseCases.Agendamentos
{
    public class CriarAgendamentoUseCase : ICriarAgendamentoUseCase
    {
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IServicoRepository _servicos;
        private readonly IRecursoRepository _recursos;
        private readonly IClienteRepository _clientes;
        private readonly ITenantRepository _tenants;
        private readonly IPagamentoRepository _pagamentos;
        private readonly ICupomRepository _cupons;
        private readonly ISaldoPacoteRepository _saldosPacote;
        private readonly IEnumerable<IGatewayPagamento> _gateways;
        private readonly IDisponibilidadeService _disponibilidade;
        private readonly IUnitOfWork _uow;

        public CriarAgendamentoUseCase(
            IAgendamentoRepository agendamentos, IServicoRepository servicos, IRecursoRepository recursos,
            IClienteRepository clientes, ITenantRepository tenants, IPagamentoRepository pagamentos,
            ICupomRepository cupons, ISaldoPacoteRepository saldosPacote,
            IEnumerable<IGatewayPagamento> gateways, IDisponibilidadeService disponibilidade, IUnitOfWork uow)
        {
            _agendamentos = agendamentos;
            _servicos = servicos;
            _recursos = recursos;
            _clientes = clientes;
            _tenants = tenants;
            _pagamentos = pagamentos;
            _cupons = cupons;
            _saldosPacote = saldosPacote;
            _gateways = gateways;
            _disponibilidade = disponibilidade;
            _uow = uow;
        }

        /// <summary>Aplica cupom ao valor total se válido. Retorna (novoValor, cupomAplicado).</summary>
        private async Task<(decimal valor, Cupom cupom)> AplicarCupomAsync(int tenantId, string codigo, decimal valorBase)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return (valorBase, null);
            var c = await _cupons.GetByCodigoAsync(tenantId, codigo);
            if (c == null || !c.EhValido(DateTime.UtcNow))
                throw new ServicoException("Cupom inválido ou expirado.");
            var novoValor = c.CalcularDesconto(valorBase);
            c.RegistrarUso();
            await _cupons.UpdateAsync(c);
            return (novoValor, c);
        }

        public async Task<CriarAgendamentoResultViewModel> ExecuteAsync(int tenantId, CriarAgendamentoInputModel input)
        {
            var tenant = await _tenants.GetByIdAsync(tenantId)
                ?? throw new TenantException("Estabelecimento não encontrado.");

            if (!tenant.TenAtivo) throw new TenantException("Estabelecimento inativo.");

            var servico = await _servicos.GetByIdAsync(input.ServicoId, tenantId)
                ?? throw new ServicoException("Serviço não encontrado.");
            if (!servico.SerAtivo) throw new ServicoException("Serviço indisponível.");

            // Validar antecedência mínima/máxima
            var dataHoraInicio = input.Data.Date.Add(input.HoraInicio);
            if (dataHoraInicio < DateTime.Now.AddHours(tenant.TenAntecedenciaMinHoras))
                throw new AgendamentoException($"O agendamento exige antecedência mínima de {tenant.TenAntecedenciaMinHoras}h.");
            if (dataHoraInicio > DateTime.Now.AddDays(tenant.TenAntecedenciaMaxDias))
                throw new AgendamentoException($"Não é permitido agendar com mais de {tenant.TenAntecedenciaMaxDias} dias de antecedência.");

            var horaFim = input.HoraInicio.Add(TimeSpan.FromMinutes(servico.SerDuracaoMinutos));

            // Resolver recurso (se não informado, escolhe primeiro disponível)
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
                var slots = await _disponibilidade.CalcularSlotsAsync(tenantId, input.ServicoId, input.Data);
                var slot = slots.FirstOrDefault(s => s.HoraInicio == input.HoraInicio);
                if (slot == null) throw new AgendamentoException("Horário indisponível.");
                recursoId = slot.RecursoId;
            }

            await _uow.BeginTransactionAsync();
            try
            {
                // Cliente: tenta achar por telefone ou email; senão cria
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

                // Validação de concorrência: verifica antes da inserção (índice único cobre o resto).
                // Aplica buffer do tenant para impedir agendamentos colados (back-to-back).
                var buffer = TimeSpan.FromMinutes(tenant.TenBufferMinutos);
                var inicioComBuffer = input.HoraInicio.Subtract(buffer);
                var fimComBuffer = horaFim.Add(buffer);
                if (await _agendamentos.ExisteConflitoAsync(tenantId, recursoId, input.Data.Date,
                        inicioComBuffer, fimComBuffer))
                {
                    throw new AgendamentoException("Horário indisponível: conflita com outro atendimento ou com o intervalo entre atendimentos.");
                }

                // ① Verifica saldo de pacote pré-pago do cliente para este serviço.
                //    Se houver, debita 1 e PULA cobrança — agendamento já fica confirmado.
                var saldoPacote = await _saldosPacote.GetSaldoValidoAsync(tenantId, cliente.CliId, servico.SerId);

                // ② Aplica cupom (se houver) ANTES da criação — afeta valor total e entrada
                var (valorComDesconto, _) = await AplicarCupomAsync(tenantId, input.CupomCodigo, servico.SerPreco);

                var agendamento = new Agendamento(tenantId, cliente.CliId, servico.SerId, recursoId,
                    input.Data, input.HoraInicio, horaFim, valorComDesconto,
                    tenant.TenPercentualEntrada, input.Observacao);

                await _agendamentos.CreateAsync(agendamento);

                Pagamento pagamento = null;
                if (saldoPacote != null && saldoPacote.Debitar())
                {
                    // Cliente tem pacote pré-pago válido — sem cobrança nova.
                    await _saldosPacote.UpdateAsync(saldoPacote);
                    agendamento.ConfirmarPagamento();
                    await _agendamentos.UpdateAsync(agendamento);
                }
                else if (input.FormaPagamento != FormaPagamento.Dinheiro)
                {
                    var gateway = _gateways.FirstOrDefault()
                        ?? throw new DomainException("Nenhum gateway de pagamento configurado.");

                    var cobranca = await gateway.CriarCobrancaAsync(tenantId, agendamento.AgeId,
                        agendamento.AgeValorEntrada, input.FormaPagamento,
                        $"Sinal - {servico.SerNome}", 15);

                    pagamento = new Pagamento(tenantId, agendamento.AgeId, input.FormaPagamento,
                        agendamento.AgeValorEntrada, gateway.Nome, cobranca.Expiracao);
                    pagamento.DefinirDadosGateway(cobranca.GatewayId, cobranca.QrCode,
                        cobranca.LinkPagamento, cobranca.PayloadBruto);
                    await _pagamentos.CreateAsync(pagamento);
                }
                else
                {
                    // Dinheiro só admin: agendamento já confirmado
                    agendamento.ConfirmarPagamento();
                    await _agendamentos.UpdateAsync(agendamento);
                }

                await _uow.CommitAsync();

                return new CriarAgendamentoResultViewModel
                {
                    Agendamento = AgendamentoMapper.Map(agendamento),
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
                throw new AgendamentoException("Horário já reservado por outro cliente.");
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<AgendamentoViewModel> ExecuteAdminAsync(int tenantId, CriarAgendamentoAdminInputModel input)
        {
            var tenant = await _tenants.GetByIdAsync(tenantId)
                ?? throw new TenantException("Estabelecimento não encontrado.");

            var servico = await _servicos.GetByIdAsync(input.ServicoId, tenantId)
                ?? throw new ServicoException("Serviço não encontrado.");

            int recursoId;
            if (input.RecursoId.HasValue)
            {
                var rec = await _recursos.GetByIdAsync(input.RecursoId.Value, tenantId)
                    ?? throw new DomainException("Recurso não encontrado.");
                recursoId = rec.RecId;
            }
            else
            {
                var slots = await _disponibilidade.CalcularSlotsAsync(tenantId, input.ServicoId, input.Data);
                var slot = slots.FirstOrDefault(s => s.HoraInicio == input.HoraInicio);
                if (slot == null) throw new AgendamentoException("Horário indisponível.");
                recursoId = slot.RecursoId;
            }

            var horaFim = input.HoraInicio.Add(TimeSpan.FromMinutes(servico.SerDuracaoMinutos));

            await _uow.BeginTransactionAsync();
            try
            {
                Cliente cliente = null;
                if (input.ClienteId.HasValue)
                {
                    cliente = await _clientes.GetByIdAsync(input.ClienteId.Value, tenantId)
                        ?? throw new DomainException("Cliente não encontrado.");
                }
                else if (input.Cliente != null)
                {
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
                }
                else
                {
                    throw new DomainException("Informe um cliente existente (ClienteId) ou novo (Cliente).");
                }

                var buffer = TimeSpan.FromMinutes(tenant.TenBufferMinutos);
                var inicioComBuffer = input.HoraInicio.Subtract(buffer);
                var fimComBuffer = horaFim.Add(buffer);
                if (await _agendamentos.ExisteConflitoAsync(tenantId, recursoId, input.Data.Date,
                        inicioComBuffer, fimComBuffer))
                {
                    throw new AgendamentoException("Horário indisponível: conflita com outro atendimento ou com o intervalo entre atendimentos.");
                }

                var valor = input.Valor.HasValue && input.Valor.Value > 0 ? input.Valor.Value : servico.SerPreco;
                var agendamento = new Agendamento(tenantId, cliente.CliId, servico.SerId, recursoId,
                    input.Data, input.HoraInicio, horaFim, valor,
                    tenant.TenPercentualEntrada, input.Observacao);

                // Admin-criado: já confirma sem precisar de cobrança.
                agendamento.ConfirmarPagamento();

                await _agendamentos.CreateAsync(agendamento);
                await _uow.CommitAsync();

                var salvo = await _agendamentos.GetByIdAsync(agendamento.AgeId, tenantId);
                return AgendamentoMapper.Map(salvo);
            }
            catch (ConcorrenciaException)
            {
                await _uow.RollbackAsync();
                throw new AgendamentoException("Horário já reservado por outro cliente.");
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
    }
}

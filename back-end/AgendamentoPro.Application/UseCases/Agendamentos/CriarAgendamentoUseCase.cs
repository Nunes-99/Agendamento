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
using Microsoft.Extensions.Logging;

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
        private readonly INotificacaoRealtime _realtime;
        private readonly IUnitOfWork _uow;
        private readonly Microsoft.Extensions.Logging.ILogger<CriarAgendamentoUseCase> _logger;

        public CriarAgendamentoUseCase(
            IAgendamentoRepository agendamentos, IServicoRepository servicos, IRecursoRepository recursos,
            IClienteRepository clientes, ITenantRepository tenants, IPagamentoRepository pagamentos,
            ICupomRepository cupons, ISaldoPacoteRepository saldosPacote,
            IEnumerable<IGatewayPagamento> gateways, IDisponibilidadeService disponibilidade,
            INotificacaoRealtime realtime, IUnitOfWork uow,
            Microsoft.Extensions.Logging.ILogger<CriarAgendamentoUseCase> logger)
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
            _realtime = realtime;
            _uow = uow;
            _logger = logger;
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

            // Dinheiro é lançamento DA OFICINA, não escolha do cliente.
            //
            // Este método atende a rota pública, que é [AllowAnonymous]. Mais abaixo,
            // FormaPagamento.Dinheiro pula o gateway e já marca o agendamento como
            // Confirmado — o que, vindo da rua, significa reservar horário sem pagar
            // nada. O sinal de 20% existe justamente para reduzir falta; sem esta
            // linha, ele é contornado com um campo no corpo da requisição, e a agenda
            // inteira pode ser ocupada de graça por qualquer um, em massa.
            //
            // O caminho legítimo do dinheiro é o ExecuteAdminAsync, atrás da policy
            // "Atendente" — lá quem lança é quem vai receber.
            if (input.FormaPagamento == FormaPagamento.Dinheiro)
                throw new DomainException(
                    "Pagamento em dinheiro é registrado pelo estabelecimento, não pelo site.");

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

            // ---------------------------------------------------------------
            // FASE 1 — só banco, transação curta.
            // ---------------------------------------------------------------
            Cliente clienteCriado;
            Agendamento agendamentoCriado;
            var precisaCobrar = true;

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
                var vaiUsarSaldo = saldoPacote != null;

                // ② Aplica cupom apenas se NÃO for usar saldo. Quando o saldo cobre o
                //    atendimento, o cupom seria "queimado" sem reduzir custo — preservar
                //    pra próximo agendamento.
                var (valorComDesconto, _) = vaiUsarSaldo
                    ? (servico.SerPreco, (Cupom)null)
                    : await AplicarCupomAsync(tenantId, input.CupomCodigo, servico.SerPreco);

                var agendamento = new Agendamento(tenantId, cliente.CliId, servico.SerId, recursoId,
                    input.Data, input.HoraInicio, horaFim, valorComDesconto,
                    tenant.TenPercentualEntrada, input.Observacao);

                await _agendamentos.CreateAsync(agendamento);

                if (saldoPacote != null && saldoPacote.Debitar())
                {
                    // Cliente tem pacote pré-pago válido — sem cobrança nova.
                    await _saldosPacote.UpdateAsync(saldoPacote);
                    agendamento.ConfirmarPagamento();
                    await _agendamentos.UpdateAsync(agendamento);
                    precisaCobrar = false;
                }

                await _uow.CommitAsync();
                clienteCriado = cliente;
                agendamentoCriado = agendamento;
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

            // ---------------------------------------------------------------
            // FASE 2 — a cobrança, FORA da transação
            //
            // A chamada ao gateway é uma ida e volta pela internet: pode levar
            // segundos, pode pendurar até o timeout. Enquanto ela estava dentro
            // da transação, cada agendamento segurava o lock de escrita do banco
            // por todo esse tempo — e no SQLite, que é o provider deste sistema,
            // só existe UM escritor por vez. Ou seja: um Mercado Pago lento
            // travava o sistema inteiro, e o sintoma ("database is locked")
            // não teria relação óbvia com a causa.
            // ---------------------------------------------------------------
            Pagamento pagamento = null;
            if (precisaCobrar)
            {
                var gateway = _gateways.FirstOrDefault(g => g.Suporta(input.FormaPagamento));
                if (gateway == null)
                {
                    await DesfazerPorFalhaDeCobrancaAsync(agendamentoCriado,
                        "Nenhum meio de pagamento disponível");
                    throw new DomainException(
                        "O pagamento online está indisponível neste estabelecimento no momento. "
                        + "Entre em contato para agendar.");
                }

                CobrancaResult cobranca;
                try
                {
                    cobranca = await gateway.CriarCobrancaAsync(tenantId, agendamentoCriado.AgeId,
                        agendamentoCriado.AgeValorEntrada, input.FormaPagamento,
                        $"Sinal - {servico.SerNome}", 15);
                }
                catch (Exception ex)
                {
                    // O horário não pode ficar preso por uma cobrança que não
                    // nasceu: desfaz o agendamento e devolve o slot para venda.
                    await DesfazerPorFalhaDeCobrancaAsync(agendamentoCriado,
                        "Falha ao iniciar a cobrança");
                    _logger.LogError(ex,
                        "Gateway {Gateway} falhou ao criar cobrança do tenant {TenantId}. "
                        + "Nenhum cliente consegue agendar enquanto isto durar.",
                        gateway.Nome, tenantId);
                    throw new DomainException(
                        "Não foi possível iniciar o pagamento agora. Tente de novo em instantes.");
                }

                // FASE 3 — grava o pagamento, de novo numa transação curta.
                await _uow.BeginTransactionAsync();
                try
                {
                    pagamento = new Pagamento(tenantId, agendamentoCriado.AgeId, input.FormaPagamento,
                        agendamentoCriado.AgeValorEntrada, gateway.Nome, cobranca.Expiracao);
                    pagamento.DefinirDadosGateway(cobranca.GatewayId, cobranca.QrCode,
                        cobranca.LinkPagamento, cobranca.PayloadBruto);
                    await _pagamentos.CreateAsync(pagamento);
                    await _uow.CommitAsync();
                }
                catch
                {
                    await _uow.RollbackAsync();
                    throw;
                }
            }

            // Notifica admins do tenant em tempo real
            _ = _realtime.NotificarTenantAsync(tenantId, "novo-agendamento", new
            {
                agendamentoId = agendamentoCriado.AgeId,
                clienteNome = clienteCriado.CliNome,
                servicoNome = servico.SerNome,
                data = agendamentoCriado.AgeData,
                horaInicio = agendamentoCriado.AgeHoraInicio,
                statusPagamento = agendamentoCriado.AgePagamentoStatus.ToString()
            });

            return new CriarAgendamentoResultViewModel
            {
                Agendamento = AgendamentoMapper.Map(agendamentoCriado),
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

        /// <summary>
        /// Cancela um agendamento cuja cobrança não vingou, devolvendo o horário
        /// para venda. Best-effort de propósito: já estamos num caminho de falha e
        /// um erro aqui não pode mascarar o erro original, que é o que interessa a
        /// quem for investigar.
        /// </summary>
        private async Task DesfazerPorFalhaDeCobrancaAsync(Agendamento agendamento, string motivo)
        {
            try
            {
                await _uow.BeginTransactionAsync();
                agendamento.Cancelar(motivo);
                await _agendamentos.UpdateAsync(agendamento);
                await _uow.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Agendamento {AgendamentoId} ficou pendente sem cobrança e não pôde ser "
                    + "cancelado. O horário pode estar bloqueado indevidamente.",
                    agendamento.AgeId);
                try { await _uow.RollbackAsync(); } catch { /* nada mais a fazer */ }
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

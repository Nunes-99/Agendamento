using AgendamentoPro.Application.Interfaces.Pagamentos;
using AgendamentoPro.Core.Entities.Pagamentos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Application.UseCases.Pagamentos
{
    public class ProcessarWebhookPagamentoUseCase : IProcessarWebhookPagamentoUseCase
    {
        private readonly IEnumerable<IGatewayPagamento> _gateways;
        private readonly IPagamentoRepository _pagamentos;
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IWebhookEventoRepository _webhooks;
        private readonly ISaldoPacoteRepository _saldosPacote;
        private readonly INotificacaoRealtime _realtime;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ProcessarWebhookPagamentoUseCase> _logger;

        public ProcessarWebhookPagamentoUseCase(IEnumerable<IGatewayPagamento> gateways,
            IPagamentoRepository pagamentos, IAgendamentoRepository agendamentos,
            IWebhookEventoRepository webhooks, ISaldoPacoteRepository saldosPacote,
            INotificacaoRealtime realtime, IUnitOfWork uow,
            ILogger<ProcessarWebhookPagamentoUseCase> logger)
        {
            _gateways = gateways;
            _pagamentos = pagamentos;
            _agendamentos = agendamentos;
            _webhooks = webhooks;
            _saldosPacote = saldosPacote;
            _realtime = realtime;
            _uow = uow;
            _logger = logger;
        }

        public async Task ExecuteAsync(string gatewayNome, string payload, string assinatura)
        {
            var gateway = _gateways.FirstOrDefault(g => g.Nome.Equals(gatewayNome, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Gateway '{gatewayNome}' não suportado.");

            var evento = await gateway.ProcessarWebhookAsync(payload, assinatura);
            if (evento == null || string.IsNullOrEmpty(evento.GatewayId)) return;

            // === Idempotência ===
            // Se eventoId não veio (gateway antigo), cai-se no comportamento anterior (sem proteção).
            // Se veio, verifica se já foi processado: se sim, ignora; se não, registra antes de processar.
            WebhookEvento registro = null;
            if (!string.IsNullOrEmpty(evento.EventoId))
            {
                var existente = await _webhooks.GetAsync(gateway.Nome, evento.EventoId);
                if (existente != null && existente.WhEvProcessadoEm.HasValue)
                {
                    _logger.LogInformation("Webhook {Gateway}/{Evento} já processado em {Quando}, ignorando duplicata.",
                        gateway.Nome, evento.EventoId, existente.WhEvProcessadoEm);
                    return;
                }
                if (existente == null)
                {
                    registro = new WebhookEvento(gateway.Nome, evento.EventoId, evento.Tipo, payload);
                    try
                    {
                        await _webhooks.CreateAsync(registro);
                    }
                    catch (Core.Exceptions.ConcorrenciaException)
                    {
                        // Race entre dois retries simultâneos — outro thread já registrou.
                        _logger.LogInformation("Webhook {Gateway}/{Evento} registrado por request concorrente, ignorando.",
                            gateway.Nome, evento.EventoId);
                        return;
                    }
                }
                else
                {
                    registro = existente;
                }
            }

            var pagamento = await _pagamentos.GetByGatewayIdAsync(evento.GatewayId);
            if (pagamento == null)
            {
                // Não achou Pagamento? Pode ser pagamento de SaldoPacote (compra de pacote
                // pré-pago). Procura saldo pendente vinculado a esse gatewayId.
                var saldo = await _saldosPacote.GetByGatewayIdAsync(evento.GatewayId);
                if (saldo != null && evento.Status == StatusPagamento.Aprovado)
                {
                    if (saldo.Ativar())
                    {
                        await _saldosPacote.UpdateAsync(saldo);
                        _logger.LogInformation("Webhook {Gateway}: SaldoPacote {SaldId} ativado.",
                            gateway.Nome, saldo.SaldId);
                    }
                    // Marca processado mesmo se Ativar() retornou false (já ativo)
                    // — caso contrário o gateway retentaria o webhook indefinidamente.
                    await FinalizarRegistroAsync(registro);
                    return;
                }

                _logger.LogWarning("Webhook {Gateway}: pagamento {GatewayId} não encontrado (nem como agendamento nem como pacote).",
                    gateway.Nome, evento.GatewayId);
                // Mesmo cenário: marca processado pra parar de retentar (não vai aparecer
                // mais — pagamento foi deletado ou nunca existiu).
                await FinalizarRegistroAsync(registro);
                return;
            }

            bool alterou = false;
            switch (evento.Status)
            {
                case StatusPagamento.Aprovado:
                    if (pagamento.Aprovar(evento.PayloadBruto))
                    {
                        var ag = await _agendamentos.GetByIdAsync(pagamento.R_AgeId, pagamento.R_TenId);
                        if (ag != null)
                        {
                            // Se este agendamento faz parte de um combo, confirma TODOS do grupo.
                            // Caso contrário, confirma apenas o agendamento isolado.
                            if (ag.AgeGrupoComboId.HasValue)
                            {
                                // Filtra por tenant (defesa em profundidade — GUID é único mas
                                // o repo não filtra). E pula itens já cancelados/concluídos
                                // para evitar exception em ConfirmarPagamento().
                                var grupo = (await _agendamentos.GetByGrupoComboAsync(ag.AgeGrupoComboId.Value))
                                    .Where(g => g.R_TenId == pagamento.R_TenId
                                        && g.AgeStatus != StatusAgendamento.Cancelado
                                        && g.AgeStatus != StatusAgendamento.Concluido);
                                foreach (var item in grupo)
                                {
                                    item.ConfirmarPagamento();
                                    await _agendamentos.UpdateAsync(item);
                                }
                            }
                            else
                            {
                                ag.ConfirmarPagamento();
                                await _agendamentos.UpdateAsync(ag);
                            }
                        }
                        alterou = true;
                    }
                    break;
                case StatusPagamento.Recusado:
                    alterou = pagamento.Recusar();
                    break;
                case StatusPagamento.Estornado:
                    alterou = pagamento.Estornar();
                    break;
                case StatusPagamento.Expirado:
                    if (pagamento.Expirar())
                    {
                        var agExp = await _agendamentos.GetByIdAsync(pagamento.R_AgeId, pagamento.R_TenId);
                        if (agExp != null)
                        {
                            agExp.ExpirarPagamento();
                            await _agendamentos.UpdateAsync(agExp);
                        }
                        alterou = true;
                    }
                    break;
            }

            if (alterou)
            {
                await _pagamentos.UpdateAsync(pagamento);
            }

            if (registro != null)
            {
                registro.MarcarProcessado();
                await _webhooks.UpdateAsync(registro);
            }

            await _uow.SaveChangesAsync();

            if (alterou && evento.Status == StatusPagamento.Aprovado)
            {
                _ = _realtime.NotificarTenantAsync(pagamento.R_TenId, "pagamento-aprovado", new
                {
                    pagamentoId = pagamento.PagId,
                    agendamentoId = pagamento.R_AgeId,
                    valor = pagamento.PagValor
                });
            }
        }

        /// <summary>
        /// Marca o WebhookEvento como processado e salva, mesmo em caminhos onde
        /// a entidade alvo já estava no estado final (idempotência). Sem isso, o
        /// gateway retentaria o webhook indefinidamente.
        /// </summary>
        private async Task FinalizarRegistroAsync(Core.Entities.Pagamentos.WebhookEvento registro)
        {
            if (registro == null) return;
            registro.MarcarProcessado();
            await _webhooks.UpdateAsync(registro);
            await _uow.SaveChangesAsync();
        }
    }
}

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
        private readonly IUnitOfWork _uow;
        private readonly ILogger<ProcessarWebhookPagamentoUseCase> _logger;

        public ProcessarWebhookPagamentoUseCase(IEnumerable<IGatewayPagamento> gateways,
            IPagamentoRepository pagamentos, IAgendamentoRepository agendamentos,
            IWebhookEventoRepository webhooks, IUnitOfWork uow,
            ILogger<ProcessarWebhookPagamentoUseCase> logger)
        {
            _gateways = gateways;
            _pagamentos = pagamentos;
            _agendamentos = agendamentos;
            _webhooks = webhooks;
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
                _logger.LogWarning("Webhook {Gateway}: pagamento {GatewayId} não encontrado no banco.",
                    gateway.Nome, evento.GatewayId);
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
                                var grupo = await _agendamentos.GetByGrupoComboAsync(ag.AgeGrupoComboId.Value);
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
        }
    }
}

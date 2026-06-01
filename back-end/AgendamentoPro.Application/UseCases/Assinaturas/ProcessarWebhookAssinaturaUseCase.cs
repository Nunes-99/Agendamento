using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Core.Entities.Assinaturas;
using AgendamentoPro.Core.Entities.Pagamentos;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Application.UseCases.Assinaturas
{
    public class ProcessarWebhookAssinaturaUseCase : IProcessarWebhookAssinaturaUseCase
    {
        private readonly IEnumerable<IGatewayAssinatura> _gateways;
        private readonly IAssinaturaRepository _assinaturas;
        private readonly IFaturaAssinaturaRepository _faturas;
        private readonly IWebhookEventoRepository _webhooks;
        private readonly IUnitOfWork _uow;
        private readonly IAssinaturaCacheInvalidator _cache;
        private readonly ILogger<ProcessarWebhookAssinaturaUseCase> _logger;

        public ProcessarWebhookAssinaturaUseCase(IEnumerable<IGatewayAssinatura> gateways,
            IAssinaturaRepository assinaturas, IFaturaAssinaturaRepository faturas,
            IWebhookEventoRepository webhooks, IUnitOfWork uow,
            IAssinaturaCacheInvalidator cache,
            ILogger<ProcessarWebhookAssinaturaUseCase> logger)
        {
            _gateways = gateways;
            _assinaturas = assinaturas;
            _faturas = faturas;
            _webhooks = webhooks;
            _uow = uow;
            _cache = cache;
            _logger = logger;
        }

        public async Task ExecuteAsync(string gatewayNome, string payload, string assinaturaHeader)
        {
            var gateway = _gateways.FirstOrDefault(g => g.Nome.Equals(gatewayNome, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Gateway de assinatura '{gatewayNome}' não suportado.");

            var evento = await gateway.ProcessarWebhookAsync(payload, assinaturaHeader);
            if (evento == null || string.IsNullOrEmpty(evento.PreapprovalId)) return;

            // Idempotência — usa namespace "MercadoPago-Assinatura" para não colidir com webhooks transacionais
            var gatewayKey = $"{gateway.Nome}-Assinatura";
            WebhookEvento registro = null;
            if (!string.IsNullOrEmpty(evento.EventoId))
            {
                var existente = await _webhooks.GetAsync(gatewayKey, evento.EventoId);
                if (existente != null && existente.WhEvProcessadoEm.HasValue)
                {
                    _logger.LogInformation("Webhook assinatura {Gw}/{Ev} já processado, ignorando.",
                        gatewayKey, evento.EventoId);
                    return;
                }
                if (existente == null)
                {
                    registro = new WebhookEvento(gatewayKey, evento.EventoId, evento.Tipo.ToString(), payload);
                    try
                    {
                        await _webhooks.CreateAsync(registro);
                    }
                    catch (Core.Exceptions.ConcorrenciaException)
                    {
                        _logger.LogInformation("Webhook assinatura {Gw}/{Ev} registrado por request concorrente.",
                            gatewayKey, evento.EventoId);
                        return;
                    }
                }
                else
                {
                    registro = existente;
                }
            }

            var assinatura = await _assinaturas.GetByGatewayPreapprovalIdAsync(evento.PreapprovalId);
            if (assinatura == null)
            {
                _logger.LogWarning("Webhook assinatura {Gw}: preapproval {Pre} não localizado localmente.",
                    gatewayKey, evento.PreapprovalId);
                await FinalizarRegistroAsync(registro);
                return;
            }

            switch (evento.Tipo)
            {
                case TipoEventoAssinatura.PreapprovalAutorizado:
                    // Cartão autorizado — nada a fazer no entity (já fica Ativa quando criada).
                    // Só persistimos o payload pra histórico se mudou o vencimento.
                    if (evento.ProximoVencimento.HasValue)
                        assinatura.DefinirPreapproval(assinatura.AssGatewayPreapprovalId,
                            evento.ProximoVencimento.Value, evento.PayloadBruto);
                    await _assinaturas.UpdateAsync(assinatura);
                    break;

                case TipoEventoAssinatura.PreapprovalCancelado:
                    if (assinatura.Cancelar(DateTime.UtcNow))
                        await _assinaturas.UpdateAsync(assinatura);
                    break;

                case TipoEventoAssinatura.PagamentoAprovado:
                {
                    var valor = evento.Valor ?? assinatura.Plano?.PlnPreco ?? 0m;
                    var pagoEm = evento.OcorreuEm ?? DateTime.UtcNow;
                    var proxVenc = evento.ProximoVencimento ?? pagoEm.AddMonths(1);

                    var fatura = await _faturas.GetByGatewayPaymentIdAsync(evento.PaymentId);
                    if (fatura == null)
                    {
                        var refInicio = assinatura.AssUltimoPagamentoEm ?? assinatura.AssDataInicio;
                        fatura = new FaturaAssinatura(assinatura.R_TenId, assinatura.AssId, valor,
                            refInicio, pagoEm, pagoEm);
                        fatura.DefinirGatewayPaymentId(evento.PaymentId, evento.PayloadBruto);
                        fatura.Pagar(pagoEm, evento.PayloadBruto);
                        await _faturas.CreateAsync(fatura);
                    }
                    else if (fatura.Pagar(pagoEm, evento.PayloadBruto))
                    {
                        await _faturas.UpdateAsync(fatura);
                    }

                    if (assinatura.RegistrarPagamento(pagoEm, proxVenc))
                        await _assinaturas.UpdateAsync(assinatura);
                    break;
                }

                case TipoEventoAssinatura.PagamentoRecusado:
                {
                    var valor = evento.Valor ?? assinatura.Plano?.PlnPreco ?? 0m;
                    var quando = evento.OcorreuEm ?? DateTime.UtcNow;

                    var fatura = await _faturas.GetByGatewayPaymentIdAsync(evento.PaymentId);
                    if (fatura == null)
                    {
                        var refInicio = assinatura.AssUltimoPagamentoEm ?? assinatura.AssDataInicio;
                        fatura = new FaturaAssinatura(assinatura.R_TenId, assinatura.AssId, valor,
                            refInicio, quando, quando);
                        fatura.DefinirGatewayPaymentId(evento.PaymentId, evento.PayloadBruto);
                        fatura.Recusar(evento.PayloadBruto);
                        await _faturas.CreateAsync(fatura);
                    }
                    else if (fatura.Recusar(evento.PayloadBruto))
                    {
                        await _faturas.UpdateAsync(fatura);
                    }

                    if (assinatura.MarcarAtrasada(quando))
                    {
                        await _assinaturas.UpdateAsync(assinatura);
                        _logger.LogWarning("Tenant {Tid} marcado como Atrasada (cobrança recusada).",
                            assinatura.R_TenId);
                    }
                    break;
                }

                case TipoEventoAssinatura.PagamentoEstornado:
                {
                    var fatura = await _faturas.GetByGatewayPaymentIdAsync(evento.PaymentId);
                    if (fatura != null && fatura.Estornar())
                        await _faturas.UpdateAsync(fatura);
                    break;
                }

                case TipoEventoAssinatura.PreapprovalPausado:
                    if (assinatura.MarcarAtrasada(evento.OcorreuEm ?? DateTime.UtcNow))
                        await _assinaturas.UpdateAsync(assinatura);
                    break;
            }

            _cache.Invalidar(assinatura.R_TenId);
            await FinalizarRegistroAsync(registro);
        }

        private async Task FinalizarRegistroAsync(WebhookEvento registro)
        {
            if (registro != null)
            {
                registro.MarcarProcessado();
                await _webhooks.UpdateAsync(registro);
            }
            await _uow.SaveChangesAsync();
        }
    }
}

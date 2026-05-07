using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Pagamentos;
using AgendamentoPro.Core.Entities.Recursos;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Agendamentos
{
    /// <summary>
    /// Núcleo do sistema. Representa o agendamento de um serviço para um cliente,
    /// em um recurso e janela de tempo específicos. Gerencia o ciclo de vida (status),
    /// pagamento, reagendamento e cancelamento.
    /// </summary>
    public class Agendamento : ITenantScoped
    {
        public int AgeId { get; private set; }
        public int R_TenId { get; private set; }
        public int R_CliId { get; private set; }
        public int R_SerId { get; private set; }
        public int R_RecId { get; private set; }
        public DateTime AgeData { get; private set; }
        public TimeSpan AgeHoraInicio { get; private set; }
        public TimeSpan AgeHoraFim { get; private set; }
        public StatusAgendamento AgeStatus { get; private set; }
        public StatusPagamento AgePagamentoStatus { get; private set; }
        public decimal AgeValorTotal { get; private set; }
        public decimal AgeValorEntrada { get; private set; }
        public decimal AgePercentualEntrada { get; private set; }
        public string AgeObservacao { get; private set; }
        public string AgeMotivoCancelamento { get; private set; }
        public DateTime? AgeCanceladoEm { get; private set; }
        public DateTime AgeCriadoEm { get; private set; }
        public DateTime? AgeAtualizadoEm { get; private set; }

        public Tenant Tenant { get; private set; }
        public Cliente Cliente { get; private set; }
        public Servico Servico { get; private set; }
        public Recurso Recurso { get; private set; }
        public List<Pagamento> Pagamentos { get; private set; }

        protected Agendamento()
        {
            Pagamentos = new List<Pagamento>();
        }

        public Agendamento(int rTenId, int rCliId, int rSerId, int rRecId,
            DateTime data, TimeSpan horaInicio, TimeSpan horaFim,
            decimal valorTotal, decimal percentualEntrada, string observacao)
        {
            Pagamentos = new List<Pagamento>();
            R_TenId = rTenId;
            R_CliId = rCliId;
            R_SerId = rSerId;
            R_RecId = rRecId;
            AgeData = data.Date;
            AgeHoraInicio = horaInicio;
            AgeHoraFim = horaFim;
            AgeValorTotal = valorTotal;
            AgePercentualEntrada = percentualEntrada;
            AgeValorEntrada = Math.Round(valorTotal * percentualEntrada / 100m, 2);
            AgeObservacao = observacao;
            AgeStatus = StatusAgendamento.PendentePagamento;
            AgePagamentoStatus = StatusPagamento.Pendente;
            AgeCriadoEm = DateTime.UtcNow;
            Validate();
        }

        public void ConfirmarPagamento()
        {
            if (AgeStatus == StatusAgendamento.Cancelado)
                throw new AgendamentoException("Não é possível confirmar pagamento de agendamento cancelado.");

            // Idempotência: se já estava aprovado, não reaplica
            if (AgePagamentoStatus == StatusPagamento.Aprovado)
                return;

            AgePagamentoStatus = StatusPagamento.Aprovado;

            // Só promove o status do agendamento se ainda estava aguardando pagamento.
            // Evita rebaixar de EmAndamento/Concluido caso webhook chegue tardiamente.
            if (AgeStatus == StatusAgendamento.PendentePagamento)
                AgeStatus = StatusAgendamento.Confirmado;

            AgeAtualizadoEm = DateTime.UtcNow;
        }

        public void IniciarAtendimento()
        {
            if (AgeStatus != StatusAgendamento.Confirmado)
                throw new AgendamentoException("Apenas agendamentos confirmados podem ser iniciados.");
            AgeStatus = StatusAgendamento.EmAndamento;
            AgeAtualizadoEm = DateTime.UtcNow;
        }

        public void Concluir()
        {
            if (AgeStatus != StatusAgendamento.EmAndamento && AgeStatus != StatusAgendamento.Confirmado)
                throw new AgendamentoException("Agendamento não pode ser concluído neste estado.");
            AgeStatus = StatusAgendamento.Concluido;
            AgeAtualizadoEm = DateTime.UtcNow;
        }

        public void Cancelar(string motivo)
        {
            if (AgeStatus == StatusAgendamento.Concluido)
                throw new AgendamentoException("Agendamento já concluído não pode ser cancelado.");
            AgeStatus = StatusAgendamento.Cancelado;
            AgeMotivoCancelamento = motivo;
            AgeCanceladoEm = DateTime.UtcNow;
            AgeAtualizadoEm = DateTime.UtcNow;
        }

        public void MarcarNoShow()
        {
            AgeStatus = StatusAgendamento.NoShow;
            AgeAtualizadoEm = DateTime.UtcNow;
        }

        public void ExpirarPagamento()
        {
            if (AgePagamentoStatus == StatusPagamento.Pendente)
            {
                AgePagamentoStatus = StatusPagamento.Expirado;
                AgeStatus = StatusAgendamento.Cancelado;
                AgeMotivoCancelamento = "Pagamento expirado.";
                AgeCanceladoEm = DateTime.UtcNow;
                AgeAtualizadoEm = DateTime.UtcNow;
            }
        }

        public void Reagendar(DateTime novaData, TimeSpan novoHoraInicio, TimeSpan novoHoraFim)
        {
            if (AgeStatus != StatusAgendamento.Confirmado && AgeStatus != StatusAgendamento.PendentePagamento)
                throw new AgendamentoException("Somente agendamentos confirmados ou pendentes podem ser reagendados.");
            AgeData = novaData.Date;
            AgeHoraInicio = novoHoraInicio;
            AgeHoraFim = novoHoraFim;
            AgeAtualizadoEm = DateTime.UtcNow;
        }

        public void AdicionarPagamento(Pagamento pagamento)
        {
            Pagamentos.Add(pagamento);
        }

        public DateTime DataHoraInicio => AgeData.Date.Add(AgeHoraInicio);
        public DateTime DataHoraFim => AgeData.Date.Add(AgeHoraFim);

        private void Validate()
        {
            if (R_TenId <= 0) throw new AgendamentoException("Tenant é obrigatório.");
            if (R_CliId <= 0) throw new AgendamentoException("Cliente é obrigatório.");
            if (R_SerId <= 0) throw new AgendamentoException("Serviço é obrigatório.");
            if (R_RecId <= 0) throw new AgendamentoException("Recurso é obrigatório.");
            if (AgeHoraInicio >= AgeHoraFim)
                throw new AgendamentoException("Hora de início deve ser anterior à hora de fim.");
            if (AgeValorTotal < 0)
                throw new AgendamentoException("Valor total não pode ser negativo.");
        }
    }
}

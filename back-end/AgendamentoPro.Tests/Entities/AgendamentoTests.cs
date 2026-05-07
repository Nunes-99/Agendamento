using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using FluentAssertions;

namespace AgendamentoPro.Tests.Entities
{
    public class AgendamentoTests
    {
        private static Agendamento Novo(StatusAgendamento? forcarStatus = null)
        {
            var ag = new Agendamento(1, 1, 1, 1,
                DateTime.Today.AddDays(2), TimeSpan.FromHours(10), TimeSpan.FromHours(11),
                100m, 20m, "obs");

            if (forcarStatus.HasValue)
            {
                // helper: encena transição via APIs públicas para chegar no status pedido
                if (forcarStatus == StatusAgendamento.Confirmado || forcarStatus == StatusAgendamento.EmAndamento || forcarStatus == StatusAgendamento.Concluido)
                {
                    ag.ConfirmarPagamento();
                }
                if (forcarStatus == StatusAgendamento.EmAndamento)
                    ag.IniciarAtendimento();
                if (forcarStatus == StatusAgendamento.Concluido)
                {
                    ag.IniciarAtendimento();
                    ag.Concluir();
                }
                if (forcarStatus == StatusAgendamento.Cancelado)
                    ag.Cancelar("teste");
            }
            return ag;
        }

        [Fact]
        public void Construtor_HoraInicioMaiorQueFim_DeveLancar()
        {
            Action act = () => new Agendamento(1, 1, 1, 1,
                DateTime.Today, TimeSpan.FromHours(11), TimeSpan.FromHours(10), 100m, 20m, null);
            act.Should().Throw<AgendamentoException>().WithMessage("*início*");
        }

        [Fact]
        public void Construtor_CalculaValorEntradaCorretamente()
        {
            var ag = new Agendamento(1, 1, 1, 1,
                DateTime.Today, TimeSpan.FromHours(10), TimeSpan.FromHours(11),
                valorTotal: 250m, percentualEntrada: 20m, observacao: null);
            ag.AgeValorEntrada.Should().Be(50m);
        }

        [Fact]
        public void ConfirmarPagamento_TransicionaParaConfirmado()
        {
            var ag = Novo();
            ag.ConfirmarPagamento();
            ag.AgeStatus.Should().Be(StatusAgendamento.Confirmado);
            ag.AgePagamentoStatus.Should().Be(StatusPagamento.Aprovado);
        }

        [Fact]
        public void ConfirmarPagamento_DuasVezes_NaoRebaixaDeEmAndamento()
        {
            var ag = Novo(StatusAgendamento.EmAndamento);
            ag.ConfirmarPagamento();
            ag.AgeStatus.Should().Be(StatusAgendamento.EmAndamento);
        }

        [Fact]
        public void ConfirmarPagamento_AgendamentoCancelado_DeveLancar()
        {
            var ag = Novo(StatusAgendamento.Cancelado);
            Action act = () => ag.ConfirmarPagamento();
            act.Should().Throw<AgendamentoException>();
        }

        [Fact]
        public void IniciarAtendimento_SemConfirmacao_DeveLancar()
        {
            var ag = Novo();
            Action act = () => ag.IniciarAtendimento();
            act.Should().Throw<AgendamentoException>();
        }

        [Fact]
        public void Concluir_AgendamentoEmAndamento_TransicionaParaConcluido()
        {
            var ag = Novo(StatusAgendamento.EmAndamento);
            ag.Concluir();
            ag.AgeStatus.Should().Be(StatusAgendamento.Concluido);
        }

        [Fact]
        public void Concluir_AgendamentoConcluido_DeveLancar()
        {
            var ag = Novo(StatusAgendamento.Concluido);
            Action act = () => ag.Concluir();
            act.Should().Throw<AgendamentoException>();
        }

        [Fact]
        public void Cancelar_AgendamentoConcluido_DeveLancar()
        {
            var ag = Novo(StatusAgendamento.Concluido);
            Action act = () => ag.Cancelar("teste");
            act.Should().Throw<AgendamentoException>();
        }

        [Fact]
        public void Reagendar_AgendamentoEmAndamento_DeveLancar()
        {
            var ag = Novo(StatusAgendamento.EmAndamento);
            Action act = () => ag.Reagendar(DateTime.Today.AddDays(5), TimeSpan.FromHours(14), TimeSpan.FromHours(15));
            act.Should().Throw<AgendamentoException>();
        }

        [Fact]
        public void Reagendar_AgendamentoConfirmado_AtualizaDataEHora()
        {
            var ag = Novo(StatusAgendamento.Confirmado);
            var novaData = DateTime.Today.AddDays(5);
            ag.Reagendar(novaData, TimeSpan.FromHours(14), TimeSpan.FromHours(15));
            ag.AgeData.Should().Be(novaData.Date);
            ag.AgeHoraInicio.Should().Be(TimeSpan.FromHours(14));
            ag.AgeHoraFim.Should().Be(TimeSpan.FromHours(15));
        }

        [Fact]
        public void ExpirarPagamento_PendentePagamento_CancelaAgendamento()
        {
            var ag = Novo();
            ag.ExpirarPagamento();
            ag.AgePagamentoStatus.Should().Be(StatusPagamento.Expirado);
            ag.AgeStatus.Should().Be(StatusAgendamento.Cancelado);
            ag.AgeMotivoCancelamento.Should().Contain("expirado");
        }

        [Fact]
        public void ExpirarPagamento_PagamentoAprovado_NaoAlteraEstado()
        {
            var ag = Novo();
            ag.ConfirmarPagamento();
            ag.ExpirarPagamento();
            ag.AgePagamentoStatus.Should().Be(StatusPagamento.Aprovado);
            ag.AgeStatus.Should().Be(StatusAgendamento.Confirmado);
        }
    }
}

using AgendamentoPro.Application.Interfaces.Agendamentos;
using AgendamentoPro.Application.UseCases.Agendamentos;
using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using FluentAssertions;
using Moq;

namespace AgendamentoPro.Tests.UseCases
{
    /// <summary>
    /// Cobre o ciclo de status do agendamento + integração com fidelidade:
    /// Concluir credita 10 pontos; Iniciar/NoShow não creditam.
    /// </summary>
    public class AlterarStatusAgendamentoUseCaseTests
    {
        private const int TenantId = 1;
        private const int ClienteId = 7;

        private readonly Mock<IAgendamentoRepository> _agendamentos = new();
        private readonly Mock<IAvaliacaoUseCase> _avaliacoes = new();
        private readonly Mock<IPontosFidelidadeRepository> _pontos = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        private AlterarStatusAgendamentoUseCase Criar() => new(
            _agendamentos.Object, _avaliacoes.Object, _pontos.Object, _uow.Object);

        private static Agendamento NovoAgendamento(StatusAgendamento status)
        {
            var ag = new Agendamento(TenantId, ClienteId, rSerId: 1, rRecId: 1,
                DateTime.Today.AddDays(1), TimeSpan.FromHours(10), TimeSpan.FromHours(11),
                valorTotal: 100m, percentualEntrada: 20m, observacao: null);

            // Driva o agendamento até o status alvo via API pública.
            if (status == StatusAgendamento.PendentePagamento) return ag;
            ag.ConfirmarPagamento();
            if (status == StatusAgendamento.Confirmado) return ag;
            ag.IniciarAtendimento();
            if (status == StatusAgendamento.EmAndamento) return ag;
            ag.Concluir();
            return ag;
        }

        [Fact]
        public async Task Concluir_ClienteSemPontosPrevios_CriaRegistroEcreditaDez()
        {
            var ag = NovoAgendamento(StatusAgendamento.EmAndamento);
            _agendamentos.Setup(r => r.GetByIdAsync(It.IsAny<int>(), TenantId)).ReturnsAsync(ag);
            _pontos.Setup(r => r.GetAsync(TenantId, ClienteId)).ReturnsAsync((PontosFidelidade)null);
            _avaliacoes.Setup(a => a.AbrirAsync(TenantId, It.IsAny<int>())).ReturnsAsync(Guid.NewGuid());

            PontosFidelidade criado = null;
            _pontos.Setup(r => r.CreateAsync(It.IsAny<PontosFidelidade>()))
                .Callback<PontosFidelidade>(p => criado = p)
                .ReturnsAsync(1);

            var uc = Criar();
            var vm = await uc.ConcluirAsync(TenantId, 99);

            vm.Status.Should().Be(StatusAgendamento.Concluido);
            _pontos.Verify(r => r.CreateAsync(It.IsAny<PontosFidelidade>()), Times.Once);
            _pontos.Verify(r => r.UpdateAsync(It.IsAny<PontosFidelidade>()), Times.Never);
            criado!.PtsSaldo.Should().Be(10);
            criado.R_TenId.Should().Be(TenantId);
            criado.R_CliId.Should().Be(ClienteId);
        }

        [Fact]
        public async Task Concluir_ClienteComPontosPrevios_AdicionaDezAoSaldoExistente()
        {
            var ag = NovoAgendamento(StatusAgendamento.EmAndamento);
            var existentes = new PontosFidelidade(TenantId, ClienteId);
            existentes.Creditar(25);

            _agendamentos.Setup(r => r.GetByIdAsync(It.IsAny<int>(), TenantId)).ReturnsAsync(ag);
            _pontos.Setup(r => r.GetAsync(TenantId, ClienteId)).ReturnsAsync(existentes);
            _avaliacoes.Setup(a => a.AbrirAsync(TenantId, It.IsAny<int>())).ReturnsAsync(Guid.NewGuid());

            var uc = Criar();
            await uc.ConcluirAsync(TenantId, 99);

            existentes.PtsSaldo.Should().Be(35);
            _pontos.Verify(r => r.UpdateAsync(existentes), Times.Once);
            _pontos.Verify(r => r.CreateAsync(It.IsAny<PontosFidelidade>()), Times.Never);
        }

        [Fact]
        public async Task Concluir_TambemAbreAvaliacaoERetornaToken()
        {
            var ag = NovoAgendamento(StatusAgendamento.EmAndamento);
            var tokenEsperado = Guid.NewGuid();

            _agendamentos.Setup(r => r.GetByIdAsync(It.IsAny<int>(), TenantId)).ReturnsAsync(ag);
            _pontos.Setup(r => r.GetAsync(TenantId, ClienteId)).ReturnsAsync(new PontosFidelidade(TenantId, ClienteId));
            _avaliacoes.Setup(a => a.AbrirAsync(TenantId, It.IsAny<int>())).ReturnsAsync(tokenEsperado);

            var uc = Criar();
            var vm = await uc.ConcluirAsync(TenantId, 99);

            vm.AvaliacaoToken.Should().Be(tokenEsperado);
            _avaliacoes.Verify(a => a.AbrirAsync(TenantId, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task Iniciar_NaoCreditaPontos()
        {
            var ag = NovoAgendamento(StatusAgendamento.Confirmado);
            _agendamentos.Setup(r => r.GetByIdAsync(It.IsAny<int>(), TenantId)).ReturnsAsync(ag);

            var uc = Criar();
            await uc.IniciarAsync(TenantId, 99);

            _pontos.Verify(r => r.CreateAsync(It.IsAny<PontosFidelidade>()), Times.Never);
            _pontos.Verify(r => r.UpdateAsync(It.IsAny<PontosFidelidade>()), Times.Never);
        }

        [Fact]
        public async Task NoShow_NaoCreditaPontos()
        {
            var ag = NovoAgendamento(StatusAgendamento.Confirmado);
            _agendamentos.Setup(r => r.GetByIdAsync(It.IsAny<int>(), TenantId)).ReturnsAsync(ag);

            var uc = Criar();
            await uc.NoShowAsync(TenantId, 99);

            _pontos.Verify(r => r.CreateAsync(It.IsAny<PontosFidelidade>()), Times.Never);
            _pontos.Verify(r => r.UpdateAsync(It.IsAny<PontosFidelidade>()), Times.Never);
        }

        [Fact]
        public async Task Concluir_SegundaChamada_LancaENaoCreditaPontosDeNovo()
        {
            var ag = NovoAgendamento(StatusAgendamento.Concluido); // já concluído
            _agendamentos.Setup(r => r.GetByIdAsync(It.IsAny<int>(), TenantId)).ReturnsAsync(ag);

            var uc = Criar();
            Func<Task> act = async () => await uc.ConcluirAsync(TenantId, 99);

            await act.Should().ThrowAsync<AgendamentoException>();
            _pontos.Verify(r => r.CreateAsync(It.IsAny<PontosFidelidade>()), Times.Never);
            _pontos.Verify(r => r.UpdateAsync(It.IsAny<PontosFidelidade>()), Times.Never);
        }

        [Fact]
        public async Task ConcluirOuOutrosStatus_AgendamentoInexistente_Lanca()
        {
            _agendamentos.Setup(r => r.GetByIdAsync(It.IsAny<int>(), TenantId))
                .ReturnsAsync((Agendamento)null);

            var uc = Criar();
            await uc.Invoking(u => u.ConcluirAsync(TenantId, 404)).Should().ThrowAsync<AgendamentoException>();
            await uc.Invoking(u => u.IniciarAsync(TenantId, 404)).Should().ThrowAsync<AgendamentoException>();
            await uc.Invoking(u => u.NoShowAsync(TenantId, 404)).Should().ThrowAsync<AgendamentoException>();
            await uc.Invoking(u => u.ConfirmarAsync(TenantId, 404)).Should().ThrowAsync<AgendamentoException>();
        }
    }
}

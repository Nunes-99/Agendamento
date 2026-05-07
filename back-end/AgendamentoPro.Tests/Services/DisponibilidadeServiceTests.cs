using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Entities.Horarios;
using AgendamentoPro.Core.Entities.Recursos;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Infrastructure.Services.Tenant;
using FluentAssertions;
using Moq;

namespace AgendamentoPro.Tests.Services
{
    public class DisponibilidadeServiceTests
    {
        private readonly Mock<ITenantRepository> _tenants = new();
        private readonly Mock<IServicoRepository> _servicos = new();
        private readonly Mock<IRecursoRepository> _recursos = new();
        private readonly Mock<IAgendamentoRepository> _agendamentos = new();
        private readonly Mock<IHorarioFuncionamentoRepository> _horarios = new();

        private DisponibilidadeService Sut() => new(_tenants.Object, _servicos.Object,
            _recursos.Object, _agendamentos.Object, _horarios.Object);

        private static Tenant TenantPadrao()
        {
            // Constructor define defaults: TenBufferMinutos=10, TenAntecedenciaMinHoras=1
            return new Tenant("X", "x", null, "x@y.com", null);
        }

        private static Servico ServicoPadrao(int duracaoMin = 60)
            => new(1, "Lavagem", null, 100m, duracaoMin, null, null, 0);

        private static Recurso RecursoPadrao()
            => new(1, "Box A", null, "Box", null, 0);

        private static HorarioFuncionamento HorarioPadrao()
            => new(1, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18),
                pausaInicio: null, pausaFim: null, aberto: true);

        [Fact]
        public async Task NaoAberto_RetornaListaVazia()
        {
            _tenants.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(TenantPadrao());
            _servicos.Setup(s => s.GetByIdAsync(1, 1)).ReturnsAsync(ServicoPadrao());
            _horarios.Setup(h => h.GetByDiaAsync(1, It.IsAny<DayOfWeek>()))
                .ReturnsAsync(new HorarioFuncionamento(1, DayOfWeek.Sunday,
                    TimeSpan.Zero, TimeSpan.Zero, null, null, aberto: false));

            var slots = await Sut().CalcularSlotsAsync(1, 1, DateTime.Today.AddDays(7));
            slots.Should().BeEmpty();
        }

        [Fact]
        public async Task DiaAbertoSemAgendamentos_GeraSlotsAcadaQuinzeMinutos()
        {
            // Quarta-feira fictícia sem conflitos: 9-18h, duração 60min
            var dataQuarta = ProximoDiaSemana(DayOfWeek.Wednesday);

            _tenants.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(TenantPadrao());
            _servicos.Setup(s => s.GetByIdAsync(1, 1)).ReturnsAsync(ServicoPadrao(60));
            _horarios.Setup(h => h.GetByDiaAsync(1, It.IsAny<DayOfWeek>()))
                .ReturnsAsync(new HorarioFuncionamento(1, DayOfWeek.Wednesday,
                    TimeSpan.FromHours(9), TimeSpan.FromHours(18), null, null, true));
            _horarios.Setup(h => h.GetBloqueiosAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Array.Empty<BloqueioAgenda>());
            _recursos.Setup(r => r.GetByTenantAsync(1, true))
                .ReturnsAsync(new[] { RecursoPadrao() });
            _agendamentos.Setup(a => a.GetByPeriodoAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
                .ReturnsAsync(Array.Empty<Agendamento>());

            var slots = (await Sut().CalcularSlotsAsync(1, 1, dataQuarta)).ToList();

            // Slots a cada 15min de 09:00 até 17:00 (último slot que termina às 18:00) = 33 slots
            slots.Should().NotBeEmpty();
            slots.First().HoraInicio.Should().Be(TimeSpan.FromHours(9));
            slots.Last().HoraFim.Should().BeLessThanOrEqualTo(TimeSpan.FromHours(18));
            slots.Should().OnlyContain(s => s.HoraFim.Subtract(s.HoraInicio) == TimeSpan.FromHours(1));
        }

        [Fact]
        public async Task ComAgendamentoExistente_RemoveSlotsConflitantesIncluindoBuffer()
        {
            var dataQuarta = ProximoDiaSemana(DayOfWeek.Wednesday);
            var ag = new Agendamento(1, 1, 1, 1, dataQuarta,
                TimeSpan.FromHours(10), TimeSpan.FromHours(11), 100m, 20m, null);

            _tenants.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(TenantPadrao());
            _servicos.Setup(s => s.GetByIdAsync(1, 1)).ReturnsAsync(ServicoPadrao(60));
            _horarios.Setup(h => h.GetByDiaAsync(1, It.IsAny<DayOfWeek>()))
                .ReturnsAsync(new HorarioFuncionamento(1, DayOfWeek.Wednesday,
                    TimeSpan.FromHours(9), TimeSpan.FromHours(18), null, null, true));
            _horarios.Setup(h => h.GetBloqueiosAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Array.Empty<BloqueioAgenda>());
            _recursos.Setup(r => r.GetByTenantAsync(1, true))
                .ReturnsAsync(new[] { RecursoPadrao() });
            _agendamentos.Setup(a => a.GetByPeriodoAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
                .ReturnsAsync(new[] { ag });

            var slots = await Sut().CalcularSlotsAsync(1, 1, dataQuarta);

            // Buffer 10min: slot que iniciaria em 09:00-10:00 colide com 09:50-10:00 (10:00-10:10 buffer
            // do existente). E qualquer slot em 09:50–11:10 deve ser excluído.
            slots.Should().NotContain(s => s.HoraInicio < TimeSpan.FromHours(11).Add(TimeSpan.FromMinutes(10))
                && s.HoraFim > TimeSpan.FromHours(10).Subtract(TimeSpan.FromMinutes(10)));
        }

        [Fact]
        public async Task PausaParaAlmoco_NaoGeraSlotsNoIntervalo()
        {
            var dataQuarta = ProximoDiaSemana(DayOfWeek.Wednesday);

            _tenants.Setup(t => t.GetByIdAsync(1)).ReturnsAsync(TenantPadrao());
            _servicos.Setup(s => s.GetByIdAsync(1, 1)).ReturnsAsync(ServicoPadrao(60));
            _horarios.Setup(h => h.GetByDiaAsync(1, It.IsAny<DayOfWeek>()))
                .ReturnsAsync(new HorarioFuncionamento(1, DayOfWeek.Wednesday,
                    TimeSpan.FromHours(9), TimeSpan.FromHours(18),
                    pausaInicio: TimeSpan.FromHours(12), pausaFim: TimeSpan.FromHours(13),
                    aberto: true));
            _horarios.Setup(h => h.GetBloqueiosAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Array.Empty<BloqueioAgenda>());
            _recursos.Setup(r => r.GetByTenantAsync(1, true))
                .ReturnsAsync(new[] { RecursoPadrao() });
            _agendamentos.Setup(a => a.GetByPeriodoAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
                .ReturnsAsync(Array.Empty<Agendamento>());

            var slots = await Sut().CalcularSlotsAsync(1, 1, dataQuarta);

            // Nenhum slot pode iniciar de modo a sobrepor 12-13h
            slots.Should().NotContain(s =>
                s.HoraInicio < TimeSpan.FromHours(13) && s.HoraFim > TimeSpan.FromHours(12));
        }

        private static DateTime ProximoDiaSemana(DayOfWeek alvo)
        {
            var d = DateTime.Today.AddDays(7);
            while (d.DayOfWeek != alvo) d = d.AddDays(1);
            return d;
        }
    }
}

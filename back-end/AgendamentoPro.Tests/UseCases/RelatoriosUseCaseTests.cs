using AgendamentoPro.Application.UseCases.Relatorios;
using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Recursos;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using FluentAssertions;
using Moq;

namespace AgendamentoPro.Tests.UseCases
{
    /// <summary>
    /// Cobre os 3 relatórios avançados (LTV, no-show por bucket, sazonalidade).
    /// Usa Moq pra IAgendamentoRepository e popula entidades via construtor +
    /// reflexão pra forçar status terminais (Concluido/NoShow) que dependem
    /// de transição encadeada.
    /// </summary>
    public class RelatoriosUseCaseTests
    {
        private const int TenantId = 1;
        private readonly Mock<IAgendamentoRepository> _agendamentos = new();
        private readonly Mock<IRecursoRepository> _recursos = new();
        private readonly Mock<IHorarioFuncionamentoRepository> _horarios = new();

        private RelatoriosUseCase Criar() => new(_agendamentos.Object, _recursos.Object, _horarios.Object);

        private static Cliente NovoCliente(int id, string nome, string tel)
        {
            var c = new Cliente(TenantId, nome, $"{nome}@x.com", tel, tel, null);
            typeof(Cliente).GetProperty("CliId")!.SetValue(c, id);
            return c;
        }

        private static Agendamento NovoAgendamento(int agId, Cliente cliente, DateTime data,
            TimeSpan hora, decimal valor, StatusAgendamento status)
        {
            var ag = new Agendamento(TenantId, cliente.CliId, rSerId: 1, rRecId: 1,
                data, hora, hora.Add(TimeSpan.FromHours(1)), valor, percentualEntrada: 20m, observacao: null);
            typeof(Agendamento).GetProperty("AgeId")!.SetValue(ag, agId);
            typeof(Agendamento).GetProperty("Cliente")!.SetValue(ag, cliente);
            typeof(Agendamento).GetProperty("AgeStatus")!.SetValue(ag, status);
            return ag;
        }

        [Fact]
        public async Task Ltv_TopN_AgrupaPorClienteEordenaPorReceita()
        {
            var ana = NovoCliente(10, "Ana", "11111");
            var bia = NovoCliente(20, "Bia", "22222");
            var carla = NovoCliente(30, "Carla", "33333");

            var lista = new[]
            {
                NovoAgendamento(1, ana, new DateTime(2026, 1, 5), TimeSpan.FromHours(10), 100m, StatusAgendamento.Concluido),
                NovoAgendamento(2, ana, new DateTime(2026, 1, 12), TimeSpan.FromHours(10), 150m, StatusAgendamento.Concluido),
                NovoAgendamento(3, bia, new DateTime(2026, 1, 7), TimeSpan.FromHours(10), 500m, StatusAgendamento.Concluido),
                NovoAgendamento(4, carla, new DateTime(2026, 1, 8), TimeSpan.FromHours(10), 80m, StatusAgendamento.Cancelado),
            };
            _agendamentos.Setup(r => r.GetByPeriodoAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(lista);

            var uc = Criar();
            var resultado = (await uc.LtvClientesAsync(TenantId, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31))).ToList();

            resultado.Should().HaveCount(2); // Carla foi cancelada → fora
            resultado[0].Nome.Should().Be("Bia");
            resultado[0].ReceitaTotal.Should().Be(500m);
            resultado[0].QuantidadeAgendamentos.Should().Be(1);
            resultado[0].TicketMedio.Should().Be(500m);

            resultado[1].Nome.Should().Be("Ana");
            resultado[1].ReceitaTotal.Should().Be(250m);
            resultado[1].QuantidadeAgendamentos.Should().Be(2);
            resultado[1].TicketMedio.Should().Be(125m);
            resultado[1].PrimeiroAgendamento.Should().Be(new DateTime(2026, 1, 5));
            resultado[1].UltimoAgendamento.Should().Be(new DateTime(2026, 1, 12));
        }

        [Fact]
        public async Task Ltv_TopRespeitado()
        {
            var clientes = Enumerable.Range(1, 5).Select(i => NovoCliente(i, $"C{i}", $"telefone{i}")).ToArray();
            var lista = clientes.Select((c, i) =>
                NovoAgendamento(i + 1, c, DateTime.Today, TimeSpan.FromHours(10), (i + 1) * 100m,
                    StatusAgendamento.Concluido)).ToArray();
            _agendamentos.Setup(r => r.GetByPeriodoAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(lista);

            var uc = Criar();
            var top3 = (await uc.LtvClientesAsync(TenantId, DateTime.Today.AddDays(-30), DateTime.Today, top: 3)).ToList();

            top3.Should().HaveCount(3);
            top3.Select(x => x.ReceitaTotal).Should().BeInDescendingOrder();
        }

        [Fact]
        public async Task NoShowPorDiaSemana_RetornaSeteBucketsMesmoQuandoVazios()
        {
            _agendamentos.Setup(r => r.GetByPeriodoAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(Array.Empty<Agendamento>());

            var uc = Criar();
            var resultado = (await uc.NoShowPorDiaSemanaAsync(TenantId, DateTime.Today.AddDays(-7), DateTime.Today)).ToList();

            resultado.Should().HaveCount(7);
            resultado.Select(x => x.Bucket).Should().ContainInOrder(
                "Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado");
            resultado.All(x => x.Total == 0).Should().BeTrue();
        }

        [Fact]
        public async Task NoShowPorDiaSemana_CalculaTaxaCorretamente()
        {
            var cliente = NovoCliente(1, "X", "11111");
            // Segunda-feira: 2026-01-05 — 1 no-show e 3 concluídos = 25%
            var segunda = new DateTime(2026, 1, 5);
            var lista = new[]
            {
                NovoAgendamento(1, cliente, segunda, TimeSpan.FromHours(10), 100m, StatusAgendamento.NoShow),
                NovoAgendamento(2, cliente, segunda, TimeSpan.FromHours(11), 100m, StatusAgendamento.Concluido),
                NovoAgendamento(3, cliente, segunda, TimeSpan.FromHours(12), 100m, StatusAgendamento.Concluido),
                NovoAgendamento(4, cliente, segunda, TimeSpan.FromHours(13), 100m, StatusAgendamento.Concluido),
                // Cancelado não conta — não está na fórmula
                NovoAgendamento(5, cliente, segunda, TimeSpan.FromHours(14), 100m, StatusAgendamento.Cancelado),
            };
            _agendamentos.Setup(r => r.GetByPeriodoAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(lista);

            var uc = Criar();
            var resultado = (await uc.NoShowPorDiaSemanaAsync(TenantId, segunda, segunda)).ToList();

            var seg = resultado.Single(x => x.Bucket == "Segunda");
            seg.NoShow.Should().Be(1);
            seg.Concluidos.Should().Be(3);
            seg.Total.Should().Be(4);
            seg.TaxaPercentual.Should().Be(25.0);
        }

        [Fact]
        public async Task NoShowPorHora_PulaHorasSemAgendamento()
        {
            var cliente = NovoCliente(1, "X", "11111");
            var dia = new DateTime(2026, 1, 5);
            var lista = new[]
            {
                NovoAgendamento(1, cliente, dia, TimeSpan.FromHours(10), 100m, StatusAgendamento.NoShow),
                NovoAgendamento(2, cliente, dia, TimeSpan.FromHours(14), 100m, StatusAgendamento.Concluido),
            };
            _agendamentos.Setup(r => r.GetByPeriodoAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(lista);

            var uc = Criar();
            var resultado = (await uc.NoShowPorHoraAsync(TenantId, dia, dia)).ToList();

            resultado.Should().HaveCount(2);
            resultado.Select(x => x.Bucket).Should().BeEquivalentTo(new[] { "10h", "14h" });
        }

        [Fact]
        public async Task Sazonalidade_PreencheTodosOsMesesNoIntervalo()
        {
            // Sem dados — espera 12 meses zerados
            _agendamentos.Setup(r => r.GetByPeriodoAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(Array.Empty<Agendamento>());

            var uc = Criar();
            var resultado = (await uc.SazonalidadeMensalAsync(TenantId, meses: 6)).ToList();

            resultado.Should().HaveCount(6);
            resultado.All(x => x.Quantidade == 0 && x.Receita == 0).Should().BeTrue();
            // Ordem crescente (mês mais antigo → mês mais recente)
            resultado.Select(x => x.Rotulo).Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task Sazonalidade_AgrupaPorAnoEMes()
        {
            var cliente = NovoCliente(1, "X", "11111");
            var hoje = DateTime.Today;
            var lista = new[]
            {
                NovoAgendamento(1, cliente, hoje, TimeSpan.FromHours(10), 100m, StatusAgendamento.Concluido),
                NovoAgendamento(2, cliente, hoje.AddDays(2), TimeSpan.FromHours(11), 200m, StatusAgendamento.Concluido),
            };
            _agendamentos.Setup(r => r.GetByPeriodoAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(lista);

            var uc = Criar();
            var resultado = (await uc.SazonalidadeMensalAsync(TenantId, meses: 3)).ToList();

            var mesAtual = resultado.Single(x => x.Ano == hoje.Year && x.Mes == hoje.Month);
            mesAtual.Quantidade.Should().Be(2);
            mesAtual.Receita.Should().Be(300m);
        }
    }
}

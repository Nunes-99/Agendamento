using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Recursos;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;

namespace AgendamentoPro.Infrastructure.Services.Tenant
{
    /// <summary>
    /// Popula um tenant recém-criado com dados-exemplo plausíveis (serviços, recursos extras,
    /// clientes e agendamentos espalhados nos últimos 30 dias + próximos 14).
    /// Útil para demonstração e desenvolvimento; em produção real, desativar via configuração.
    /// </summary>
    public class DemoDataSeeder : ITenantSeeder
    {
        private readonly IServicoRepository _servicos;
        private readonly IRecursoRepository _recursos;
        private readonly IClienteRepository _clientes;
        private readonly IAgendamentoRepository _agendamentos;
        private readonly ITenantRepository _tenants;

        public DemoDataSeeder(IServicoRepository servicos, IRecursoRepository recursos,
            IClienteRepository clientes, IAgendamentoRepository agendamentos, ITenantRepository tenants)
        {
            _servicos = servicos;
            _recursos = recursos;
            _clientes = clientes;
            _agendamentos = agendamentos;
            _tenants = tenants;
        }

        public async Task PopularAsync(int tenantId)
        {
            var tenant = await _tenants.GetByIdAsync(tenantId);
            if (tenant == null) return;

            // 1. Serviços (catálogo típico de lava-rápido)
            var servicos = new[]
            {
                new Servico(tenantId, "Lavagem Simples", "Lavagem externa rápida com produtos de qualidade.", 35m, 30, null, "Lavagem", 1),
                new Servico(tenantId, "Lavagem Completa", "Lavagem externa + interna + aspiração + pretinho.", 70m, 60, null, "Lavagem", 2),
                new Servico(tenantId, "Higienização Interna", "Limpeza profunda de bancos, carpetes e teto.", 180m, 120, null, "Estética", 3),
                new Servico(tenantId, "Polimento Comercial", "Remove riscos leves e devolve brilho à pintura.", 250m, 180, null, "Estética", 4),
                new Servico(tenantId, "Cera Premium", "Proteção da pintura por até 90 dias.", 120m, 60, null, "Proteção", 5),
                new Servico(tenantId, "Pacote Executivo", "Lavagem completa + cera premium + higienização interna.", 380m, 240, null, "Combo", 6)
            };
            foreach (var s in servicos) await _servicos.CreateAsync(s);

            // 2. Recursos adicionais (já existe Box 01 do tenant inicial)
            var recursosExtras = new[]
            {
                new Recurso(tenantId, "Box 02", "Box coberto", "Box", null, 2),
                new Recurso(tenantId, "Box 03", "Box coberto", "Box", null, 3),
                new Recurso(tenantId, "Box Premium", "Box com elevador para serviços de estética", "Box", null, 4)
            };
            foreach (var r in recursosExtras) await _recursos.CreateAsync(r);

            var todosRecursos = (await _recursos.GetByTenantAsync(tenantId, true)).ToList();

            // 3. Clientes (nomes brasileiros plausíveis)
            var nomes = new[]
            {
                "Carlos Silva", "Mariana Souza", "Pedro Almeida", "Ana Beatriz Costa",
                "Rafael Oliveira", "Juliana Santos", "Lucas Ferreira", "Camila Ribeiro",
                "Bruno Martins", "Fernanda Lima", "Thiago Pereira", "Patrícia Gomes",
                "Marcelo Andrade", "Beatriz Carvalho", "André Luiz Rocha", "Letícia Mendes",
                "Gustavo Barbosa", "Renata Cardoso", "Felipe Araújo", "Tatiane Nogueira",
                "Diego Cavalcanti", "Isabela Fonseca", "Rodrigo Teixeira", "Vanessa Pinto",
                "Eduardo Moura", "Caroline Dias", "Henrique Vieira", "Paula Castro",
                "Vinicius Correa", "Larissa Macedo"
            };
            var random = new Random(42); // seed fixa para reprodutibilidade
            var clientes = new List<Cliente>();
            for (int i = 0; i < nomes.Length; i++)
            {
                var nome = nomes[i];
                var primeiroNome = nome.Split(' ')[0].ToLowerInvariant();
                var sobrenome = nome.Split(' ').Last().ToLowerInvariant();
                var telefone = $"11{random.Next(90000, 99999)}{random.Next(1000, 9999)}";
                var email = $"{primeiroNome}.{sobrenome}@exemplo.com";
                var c = new Cliente(tenantId, nome, email, telefone, telefone, null);
                await _clientes.CreateAsync(c);
                clientes.Add(c);
            }

            // 4. Agendamentos com distribuição realista
            //    - 30 dias passados: maior parte Concluido, alguns Cancelado/NoShow
            //    - Próximos 14 dias: Confirmado e PendentePagamento
            var hoje = DateTime.Today;
            var horariosPossiveis = new[] {
                new TimeSpan(8, 0, 0), new TimeSpan(8, 30, 0), new TimeSpan(9, 0, 0),
                new TimeSpan(9, 30, 0), new TimeSpan(10, 0, 0), new TimeSpan(10, 30, 0),
                new TimeSpan(11, 0, 0),
                new TimeSpan(13, 30, 0), new TimeSpan(14, 0, 0), new TimeSpan(14, 30, 0),
                new TimeSpan(15, 0, 0), new TimeSpan(15, 30, 0), new TimeSpan(16, 0, 0),
                new TimeSpan(16, 30, 0), new TimeSpan(17, 0, 0)
            };

            // Passado (30 dias)
            for (int i = 0; i < 60; i++)
            {
                var diasAtras = random.Next(1, 31);
                var data = hoje.AddDays(-diasAtras);
                if (data.DayOfWeek == DayOfWeek.Sunday) data = data.AddDays(-1);

                var servico = servicos[random.Next(servicos.Length)];
                var recurso = todosRecursos[random.Next(todosRecursos.Count)];
                var cliente = clientes[random.Next(clientes.Count)];
                var hora = horariosPossiveis[random.Next(horariosPossiveis.Length)];
                var horaFim = hora.Add(TimeSpan.FromMinutes(servico.SerDuracaoMinutos));

                if (await _agendamentos.ExisteConflitoAsync(tenantId, recurso.RecId, data, hora, horaFim))
                    continue;

                var ag = new Agendamento(tenantId, cliente.CliId, servico.SerId, recurso.RecId,
                    data, hora, horaFim, servico.SerPreco, tenant.TenPercentualEntrada, null);

                ag.ConfirmarPagamento();

                var sorteio = random.Next(100);
                if (sorteio < 75) ag.Concluir();
                else if (sorteio < 88) ag.Cancelar("Cliente desistiu");
                else if (sorteio < 95) ag.MarcarNoShow();
                // resto fica como Confirmado

                await _agendamentos.CreateAsync(ag);
            }

            // Hoje + 14 dias
            for (int i = 0; i < 35; i++)
            {
                var diasFrente = random.Next(0, 15);
                var data = hoje.AddDays(diasFrente);
                if (data.DayOfWeek == DayOfWeek.Sunday) data = data.AddDays(1);

                var servico = servicos[random.Next(servicos.Length)];
                var recurso = todosRecursos[random.Next(todosRecursos.Count)];
                var cliente = clientes[random.Next(clientes.Count)];
                var hora = horariosPossiveis[random.Next(horariosPossiveis.Length)];
                var horaFim = hora.Add(TimeSpan.FromMinutes(servico.SerDuracaoMinutos));

                // Agendamentos hoje precisam ter horário no futuro
                if (data == hoje && hora < DateTime.Now.TimeOfDay) continue;

                if (await _agendamentos.ExisteConflitoAsync(tenantId, recurso.RecId, data, hora, horaFim))
                    continue;

                var ag = new Agendamento(tenantId, cliente.CliId, servico.SerId, recurso.RecId,
                    data, hora, horaFim, servico.SerPreco, tenant.TenPercentualEntrada, null);

                // 70% confirmado, 30% pendente pagamento
                if (random.Next(100) < 70) ag.ConfirmarPagamento();

                await _agendamentos.CreateAsync(ag);
            }
        }
    }
}

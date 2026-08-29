using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Entities.Clientes;
using AgendamentoPro.Core.Entities.Horarios;
using AgendamentoPro.Core.Entities.Recursos;
using AgendamentoPro.Core.Entities.Servicos;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using AgendamentoPro.Core.Interfaces.Services;
using AgendamentoPro.Infrastructure.Database.EntityFramework;

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
        private readonly IComboRepository _combos;
        private readonly IAvaliacaoRepository _avaliacoes;
        private readonly IUnitOfWork _uow;
        // Cupom, pacote, pontos, bloqueio e fila de espera não têm Create no
        // repositório — o contexto resolve sem inflar as interfaces por causa do seed.
        private readonly AgendamentoProDbContext _ctx;

        public DemoDataSeeder(IServicoRepository servicos, IRecursoRepository recursos,
            IClienteRepository clientes, IAgendamentoRepository agendamentos, ITenantRepository tenants,
            IComboRepository combos, IAvaliacaoRepository avaliacoes, IUnitOfWork uow,
            AgendamentoProDbContext ctx)
        {
            _ctx = ctx;
            _servicos = servicos;
            _recursos = recursos;
            _clientes = clientes;
            _agendamentos = agendamentos;
            _tenants = tenants;
            _combos = combos;
            _avaliacoes = avaliacoes;
            _uow = uow;
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

            // 5. Combos promocionais (catálogo)
            await SemearCombosAsync(tenantId, servicos);

            // 6. Avaliações de clientes em agendamentos concluídos
            await SemearAvaliacoesAsync(tenantId, random);

            // 7. Vitrine, cupons, pacotes, fidelidade, bloqueio e fila de espera —
            //    sem isso, metade do painel abre vazio numa demonstração.
            await SemearVitrineAsync(tenant);
            await SemearComercialAsync(tenantId, servicos, clientes, random);
        }

        /// <summary>Cores, fonte e anúncios da página pública do tenant.</summary>
        private async Task SemearVitrineAsync(Core.Entities.Tenants.Tenant tenant)
        {
            tenant.AtualizarPersonalizacao(
                tenant.TenLogoUrl, tenant.TenBannerUrl, tenant.TenFaviconUrl,
                corPrimaria: "#1565c0",   // azul — botões e preços
                corSecundaria: "#0d47a1", // azul escuro — fundo do banner
                corAcento: "#f57c00",     // laranja — promoções em destaque
                fonte: "Poppins");
            await _tenants.UpdateAsync(tenant);

            var anuncios = new[]
            {
                new { titulo = "Semana do Brilho: 20% off na Lavagem Completa",
                      texto = "De R$ 70 por R$ 56 até sexta-feira. Agende pelo site!",
                      destaque = true, ativo = true },
                new { titulo = "Sábado com café da manhã por nossa conta",
                      texto = "Enquanto seu carro brilha, você toma um café fresquinho.",
                      destaque = false, ativo = true },
                new { titulo = "Leve o Cuidado Total e economize R$ 31",
                      texto = "Lavagem completa + cera premium por R$ 159.",
                      destaque = false, ativo = true }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(anuncios,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            _ctx.ConfiguracoesTenant.Add(new ConfiguracaoTenant(
                tenant.TenId, "vitrine.anuncios", json, "vitrine", sensivel: false));

            // Galeria fica vazia de propósito: fotos são do espaço real do cliente
            // (Configurações → Minha página → Galeria de fotos).
        }

        /// <summary>Cupons, pacote pré-pago, pontos, bloqueio e fila de espera.</summary>
        private async Task SemearComercialAsync(int tenantId, Servico[] servicos,
            List<Cliente> clientes, Random random)
        {
            var hoje = DateTime.Today;

            _ctx.Cupons.Add(new Cupom(tenantId, "BEMVINDO10", TipoDesconto.Percentual, 10m,
                hoje.AddDays(-15), hoje.AddDays(60), usosMaximos: 100));
            _ctx.Cupons.Add(new Cupom(tenantId, "VOLTA20", TipoDesconto.ValorFixo, 20m,
                hoje.AddDays(-5), hoje.AddDays(30), usosMaximos: 50));

            var lavagemSimples = servicos.First(s => s.SerNome == "Lavagem Simples");
            var lavagemCompleta = servicos.First(s => s.SerNome == "Lavagem Completa");
            _ctx.PacotesPrePagos.Add(new PacotePrePago(tenantId, lavagemSimples.SerId,
                "Pacote 5 Lavagens Simples", 5, 150m, 90));   // R$ 30/lavagem vs 35 avulso
            _ctx.PacotesPrePagos.Add(new PacotePrePago(tenantId, lavagemCompleta.SerId,
                "Pacote 4 Lavagens Completas", 4, 250m, 120)); // R$ 62,50 vs 70 avulso

            // Pontos de fidelidade para os primeiros clientes — um deles já passa
            // dos 100, então a troca por cupom pode ser demonstrada na hora.
            var saldos = new[] { 120, 80, 60, 40, 30, 20, 10 };
            for (int i = 0; i < saldos.Length && i < clientes.Count; i++)
            {
                var p = new PontosFidelidade(tenantId, clientes[i].CliId);
                p.Creditar(saldos[i]);
                _ctx.PontosFidelidade.Add(p);
            }

            // Feriado à frente: mostra a agenda respeitando bloqueio.
            var proximoFeriado = hoje.AddDays(21);
            _ctx.BloqueiosAgenda.Add(new BloqueioAgenda(tenantId, null,
                proximoFeriado, proximoFeriado.AddDays(1).AddSeconds(-1), "Feriado — fechado"));

            // Fila de espera para um dia cheio.
            var dataCheia = hoje.AddDays(2);
            _ctx.ListaEspera.Add(new ListaEspera(tenantId, lavagemCompleta.SerId, dataCheia,
                "Rafael Lima", "11955443322", null, "Prefiro de manhã"));
            _ctx.ListaEspera.Add(new ListaEspera(tenantId, lavagemSimples.SerId, dataCheia,
                "Simone Prado", "11944332211", null, null));

            await _uow.SaveChangesAsync();
        }

        private async Task SemearCombosAsync(int tenantId, Servico[] servicos)
        {
            // Combo "Cuidado total" = Lavagem completa + Cera Premium
            // Soma original ~190 -> promocional 159 (16% off)
            var lavagemCompleta = servicos.FirstOrDefault(s => s.SerNome == "Lavagem Completa");
            var ceraPremium = servicos.FirstOrDefault(s => s.SerNome == "Cera Premium");
            if (lavagemCompleta != null && ceraPremium != null)
            {
                var combo = new Combo(tenantId, "Cuidado Total",
                    "Lavagem completa + Cera premium para deixar seu carro impecável.",
                    null, precoPromocional: 159m, ordem: 1);
                combo.DefinirServicos(new[] { lavagemCompleta.SerId, ceraPremium.SerId });
                await _combos.CreateAsync(combo);
            }

            // Combo "Detalhamento Completo" = Higienização interna + Polimento + Cera
            var higInterna = servicos.FirstOrDefault(s => s.SerNome == "Higienização Interna");
            var polimento = servicos.FirstOrDefault(s => s.SerNome == "Polimento Comercial");
            if (higInterna != null && polimento != null && ceraPremium != null)
            {
                var combo = new Combo(tenantId, "Detalhamento Completo",
                    "Recupera o brilho do seu carro: polimento + higienização interna + cera premium.",
                    null, precoPromocional: 489m, ordem: 2);
                combo.DefinirServicos(new[] { polimento.SerId, higInterna.SerId, ceraPremium.SerId });
                await _combos.CreateAsync(combo);
            }
        }

        private async Task SemearAvaliacoesAsync(int tenantId, Random random)
        {
            // Pega agendamentos concluídos do tenant (até ~10 mais recentes pra demo)
            var hoje = DateTime.Today;
            var concluidos = (await _agendamentos.GetByPeriodoAsync(tenantId,
                hoje.AddDays(-30), hoje, null))
                .Where(a => a.AgeStatus == StatusAgendamento.Concluido)
                .OrderByDescending(a => a.AgeData)
                .Take(12)
                .ToList();

            var comentarios = new[]
            {
                "Atendimento impecável, recomendo!",
                "Carro ficou novinho. Ótimo serviço.",
                "Profissional atencioso, voltarei sempre.",
                "Preço justo e qualidade ótima.",
                "Adorei o resultado, super recomendo.",
                "Ambiente agradável, atendimento rápido.",
                null, null, // alguns sem comentário
                "Esperei pouco e o serviço foi caprichado.",
                "Bom atendimento, mas demorou um pouco mais que o combinado.",
                "Excelente! Voltarei na próxima semana."
            };

            foreach (var ag in concluidos)
            {
                var aval = new Avaliacao(tenantId, ag.AgeId, ag.R_CliId);
                // 80% das demos respondidas com nota 4-5, 20% com nota 3
                var nota = random.Next(100) < 80 ? random.Next(4, 6) : 3;
                var comentario = comentarios[random.Next(comentarios.Length)];
                aval.Responder(nota, comentario);
                await _avaliacoes.CreateAsync(aval);
            }
        }
    }
}

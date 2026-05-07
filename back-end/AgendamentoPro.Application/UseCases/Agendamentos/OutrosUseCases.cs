using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Application.Interfaces.Agendamentos;
using AgendamentoPro.Application.ViewModels.Agendamentos;
using AgendamentoPro.Application.ViewModels.Common;
using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Enums;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;
using Microsoft.Extensions.Logging;

namespace AgendamentoPro.Application.UseCases.Agendamentos
{
    public class ConsultarAgendamentoUseCase : IConsultarAgendamentoUseCase
    {
        private readonly IAgendamentoRepository _agendamentos;
        public ConsultarAgendamentoUseCase(IAgendamentoRepository a) { _agendamentos = a; }

        public async Task<AgendamentoViewModel> PorIdAsync(int tenantId, int id)
        {
            var a = await _agendamentos.GetByIdAsync(id, tenantId);
            return a == null ? null : AgendamentoMapper.Map(a);
        }

        public async Task<IEnumerable<AgendamentoViewModel>> AgendaDoDiaAsync(int tenantId, DateTime data, int? recursoId)
        {
            var lista = await _agendamentos.GetByPeriodoAsync(tenantId, data.Date, data.Date.AddDays(1).AddTicks(-1), recursoId);
            return lista.Select(AgendamentoMapper.Map);
        }

        public async Task<IEnumerable<AgendamentoViewModel>> AgendaPorPeriodoAsync(int tenantId, DateTime inicio, DateTime fim, int? recursoId)
        {
            if (fim.Date < inicio.Date)
                throw new AgendamentoException("Data final deve ser maior ou igual à data inicial.");
            var lista = await _agendamentos.GetByPeriodoAsync(tenantId, inicio.Date, fim.Date, recursoId);
            return lista.Select(AgendamentoMapper.Map);
        }

        public async Task<PaginadoViewModel<AgendamentoViewModel>> ListarPaginadoAsync(int tenantId, int page, int pageSize, DateTime? data, StatusAgendamento? status)
        {
            var (items, total) = await _agendamentos.GetPagedAsync(tenantId, page, pageSize, data, status);
            return new PaginadoViewModel<AgendamentoViewModel>
            {
                Items = items.Select(AgendamentoMapper.Map),
                Total = total,
                Pagina = page,
                TamanhoPagina = pageSize
            };
        }

        public async Task<IEnumerable<AgendamentoViewModel>> PorGrupoComboAsync(int tenantId, Guid grupoComboId)
        {
            var lista = await _agendamentos.GetByGrupoComboAsync(grupoComboId);
            // Filtra pelo tenant (defesa em profundidade — repositório já filtra por id do grupo único).
            return lista.Where(a => a.R_TenId == tenantId)
                .OrderBy(a => a.AgeData).ThenBy(a => a.AgeHoraInicio)
                .Select(AgendamentoMapper.Map);
        }
    }

    public class ReagendarUseCase : IReagendarUseCase
    {
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IServicoRepository _servicos;
        private readonly ITenantRepository _tenants;
        private readonly IUnitOfWork _uow;

        public ReagendarUseCase(IAgendamentoRepository a, IServicoRepository s,
            ITenantRepository t, IUnitOfWork u)
        {
            _agendamentos = a;
            _servicos = s;
            _tenants = t;
            _uow = u;
        }

        public async Task<AgendamentoViewModel> ExecuteAsync(int tenantId, int id, ReagendarInputModel input)
        {
            var ag = await _agendamentos.GetByIdAsync(id, tenantId)
                ?? throw new AgendamentoException("Agendamento não encontrado.");

            // Reagendar individual quebra a sequência contígua do combo. Bloqueia
            // e orienta cancelar + criar novo combo. (Reagendamento em massa do
            // combo inteiro é candidato a feature futura.)
            if (ag.AgeGrupoComboId.HasValue)
                throw new AgendamentoException(
                    "Este agendamento faz parte de um combo. Cancele o combo inteiro e crie um novo no horário desejado.");

            var tenant = await _tenants.GetByIdAsync(tenantId);
            // Regra: só permitir reagendar com mais de N horas de antecedência (do agendamento original)
            var antecedencia = ag.DataHoraInicio - DateTime.Now;
            if (antecedencia.TotalHours < tenant.TenLimiteCancelamentoHoras)
                throw new AgendamentoException($"Reagendamentos exigem antecedência mínima de {tenant.TenLimiteCancelamentoHoras}h.");

            // Validar nova data: não pode ser no passado e respeita antecedência mínima
            var novaDataHora = input.NovaData.Date.Add(input.NovaHoraInicio);
            if (novaDataHora < DateTime.Now.AddHours(tenant.TenAntecedenciaMinHoras))
                throw new AgendamentoException($"Nova data deve respeitar antecedência mínima de {tenant.TenAntecedenciaMinHoras}h.");
            if (novaDataHora > DateTime.Now.AddDays(tenant.TenAntecedenciaMaxDias))
                throw new AgendamentoException($"Nova data ultrapassa antecedência máxima ({tenant.TenAntecedenciaMaxDias} dias).");

            var servico = await _servicos.GetByIdAsync(ag.R_SerId, tenantId);
            var horaFim = input.NovaHoraInicio.Add(TimeSpan.FromMinutes(servico.SerDuracaoMinutos));

            // Buffer entre atendimentos
            var buffer = TimeSpan.FromMinutes(tenant.TenBufferMinutos);
            var inicioComBuffer = input.NovaHoraInicio.Subtract(buffer);
            var fimComBuffer = horaFim.Add(buffer);

            if (await _agendamentos.ExisteConflitoAsync(tenantId, ag.R_RecId, input.NovaData.Date,
                inicioComBuffer, fimComBuffer, ag.AgeId))
            {
                throw new AgendamentoException("Horário indisponível para reagendamento.");
            }

            ag.Reagendar(input.NovaData, input.NovaHoraInicio, horaFim);
            await _agendamentos.UpdateAsync(ag);
            await _uow.SaveChangesAsync();
            return AgendamentoMapper.Map(ag);
        }
    }

    public class CancelarAgendamentoUseCase : ICancelarAgendamentoUseCase
    {
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IListaEsperaRepository _esperaRepo;
        private readonly Core.Interfaces.Services.INotificadorWhatsApp _whatsapp;
        private readonly Core.Interfaces.Services.INotificacaoRealtime _realtime;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
        private readonly Microsoft.Extensions.Logging.ILogger<CancelarAgendamentoUseCase> _logger;
        private readonly IUnitOfWork _uow;

        public CancelarAgendamentoUseCase(IAgendamentoRepository a,
            IListaEsperaRepository esperaRepo,
            Core.Interfaces.Services.INotificadorWhatsApp whatsapp,
            Core.Interfaces.Services.INotificacaoRealtime realtime,
            Microsoft.Extensions.Configuration.IConfiguration config,
            Microsoft.Extensions.Logging.ILogger<CancelarAgendamentoUseCase> logger,
            IUnitOfWork u)
        {
            _agendamentos = a;
            _esperaRepo = esperaRepo;
            _whatsapp = whatsapp;
            _realtime = realtime;
            _config = config;
            _logger = logger;
            _uow = u;
        }

        private async Task NotificarPrimeiroNaEsperaAsync(Agendamento ag)
        {
            try
            {
                var primeiro = await _esperaRepo.GetPrimeiroNaoNotificadoAsync(
                    ag.R_TenId, ag.R_SerId, ag.AgeData);
                if (primeiro == null) return;

                var numero = primeiro.LesClienteTelefone;
                if (!string.IsNullOrWhiteSpace(numero) && _whatsapp.Ativo)
                {
                    var slug = ag.Tenant?.TenSlug ?? "";
                    var frontUrl = (Environment.GetEnvironmentVariable("APP_FRONTEND_URL")
                        ?? _config["App:FrontendUrl"] ?? "").TrimEnd('/');
                    var msg = $"Olá {primeiro.LesClienteNome}! Vagou um horário. Agende em {frontUrl}/t/{slug}/servicos";
                    await _whatsapp.EnviarAsync(numero, msg);
                }
                primeiro.MarcarNotificado();
                await _esperaRepo.UpdateAsync(primeiro);
                await _uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao notificar lista de espera após cancelamento {Id}", ag.AgeId);
            }
        }

        public async Task<AgendamentoViewModel> ExecuteAsync(int tenantId, int id, CancelarAgendamentoInputModel input)
        {
            var ag = await _agendamentos.GetByIdAsync(id, tenantId)
                ?? throw new AgendamentoException("Agendamento não encontrado.");

            var motivo = input.Motivo ?? "Cancelado pelo usuário.";

            // Se faz parte de um combo, cancela TODOS os agendamentos do grupo.
            // Cliente pagou 1x pelo combo inteiro - não faz sentido manter pedaços.
            if (ag.AgeGrupoComboId.HasValue)
            {
                var grupo = (await _agendamentos.GetByGrupoComboAsync(ag.AgeGrupoComboId.Value))
                    .Where(g => g.R_TenId == tenantId)
                    .ToList();
                foreach (var item in grupo)
                {
                    if (item.AgeStatus != Core.Enums.StatusAgendamento.Cancelado
                        && item.AgeStatus != Core.Enums.StatusAgendamento.Concluido)
                    {
                        item.Cancelar(motivo);
                        await _agendamentos.UpdateAsync(item);
                    }
                }
                await _uow.SaveChangesAsync();
                await NotificarPrimeiroNaEsperaAsync(ag);
                return AgendamentoMapper.Map(ag);
            }

            ag.Cancelar(motivo);
            await _agendamentos.UpdateAsync(ag);
            await _uow.SaveChangesAsync();

            // Vagou um slot — notifica o primeiro da fila se houver alguém esperando
            await NotificarPrimeiroNaEsperaAsync(ag);

            // Realtime ao admin
            _ = _realtime.NotificarTenantAsync(tenantId, "agendamento-cancelado", new
            {
                agendamentoId = ag.AgeId,
                data = ag.AgeData,
                horaInicio = ag.AgeHoraInicio,
                motivo
            });

            return AgendamentoMapper.Map(ag);
        }
    }

    public class AlterarStatusAgendamentoUseCase : IAlterarStatusAgendamentoUseCase
    {
        // Cada agendamento concluído gera N pontos. Configurável por tenant via
        // `Configuracao` futura; por ora valor fixo (10 pontos por agendamento).
        // 100 pts = R$ 10 de cupom (regra em NegocioController.TrocarPorCupom).
        private const int PontosPorAgendamento = 10;

        private readonly IAgendamentoRepository _agendamentos;
        private readonly IAvaliacaoUseCase _avaliacoes;
        private readonly IPontosFidelidadeRepository _pontos;
        private readonly IUnitOfWork _uow;

        public AlterarStatusAgendamentoUseCase(IAgendamentoRepository a, IAvaliacaoUseCase av,
            IPontosFidelidadeRepository pontos, IUnitOfWork u)
        {
            _agendamentos = a;
            _avaliacoes = av;
            _pontos = pontos;
            _uow = u;
        }

        private async Task CreditarPontosFidelidadeAsync(Agendamento ag)
        {
            var pts = await _pontos.GetAsync(ag.R_TenId, ag.R_CliId);
            if (pts == null)
            {
                pts = new Core.Entities.Clientes.PontosFidelidade(ag.R_TenId, ag.R_CliId);
                pts.Creditar(PontosPorAgendamento);
                await _pontos.CreateAsync(pts);
            }
            else
            {
                pts.Creditar(PontosPorAgendamento);
                await _pontos.UpdateAsync(pts);
            }
        }

        private async Task<Agendamento> CarregarAsync(int tenantId, int id) =>
            await _agendamentos.GetByIdAsync(id, tenantId) ?? throw new AgendamentoException("Agendamento não encontrado.");

        public async Task<AgendamentoViewModel> ConfirmarAsync(int tenantId, int id)
        {
            var ag = await CarregarAsync(tenantId, id);
            ag.ConfirmarPagamento();
            await _agendamentos.UpdateAsync(ag);
            await _uow.SaveChangesAsync();
            return AgendamentoMapper.Map(ag);
        }

        public async Task<AgendamentoViewModel> IniciarAsync(int tenantId, int id)
        {
            var ag = await CarregarAsync(tenantId, id);
            ag.IniciarAtendimento();
            await _agendamentos.UpdateAsync(ag);
            await _uow.SaveChangesAsync();
            return AgendamentoMapper.Map(ag);
        }

        public async Task<AgendamentoViewModel> ConcluirAsync(int tenantId, int id)
        {
            var ag = await CarregarAsync(tenantId, id);
            ag.Concluir();
            await _agendamentos.UpdateAsync(ag);

            // Credita pontos de fidelidade ao concluir (idempotente: se Concluir
            // for chamado 2x, não chega aqui na 2ª — Agendamento.Concluir lança
            // se já estava Concluido).
            await CreditarPontosFidelidadeAsync(ag);

            await _uow.SaveChangesAsync();

            // Abre avaliação ao concluir - cliente recebe link público.
            // Idempotente: se já existe avaliação, reutiliza o token existente.
            var token = await _avaliacoes.AbrirAsync(tenantId, ag.AgeId);

            var vm = AgendamentoMapper.Map(ag);
            vm.AvaliacaoToken = token;
            return vm;
        }

        public async Task<AgendamentoViewModel> NoShowAsync(int tenantId, int id)
        {
            var ag = await CarregarAsync(tenantId, id);
            ag.MarcarNoShow();
            await _agendamentos.UpdateAsync(ag);
            await _uow.SaveChangesAsync();
            return AgendamentoMapper.Map(ag);
        }
    }
}

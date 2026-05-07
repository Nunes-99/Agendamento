using AgendamentoPro.Application.InputModels.Agendamentos;
using AgendamentoPro.Application.Interfaces.Agendamentos;
using AgendamentoPro.Application.ViewModels.Agendamentos;
using AgendamentoPro.Core.Entities.Agendamentos;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Common;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Agendamentos
{
    public class AvaliacaoUseCase : IAvaliacaoUseCase
    {
        private readonly IAvaliacaoRepository _avaliacoes;
        private readonly IAgendamentoRepository _agendamentos;
        private readonly IUnitOfWork _uow;

        public AvaliacaoUseCase(IAvaliacaoRepository avaliacoes,
            IAgendamentoRepository agendamentos, IUnitOfWork uow)
        {
            _avaliacoes = avaliacoes;
            _agendamentos = agendamentos;
            _uow = uow;
        }

        public async Task<Guid> AbrirAsync(int tenantId, int agendamentoId)
        {
            var existente = await _avaliacoes.GetByAgendamentoAsync(agendamentoId);
            if (existente != null) return existente.AvaToken;

            var ag = await _agendamentos.GetByIdAsync(agendamentoId, tenantId)
                ?? throw new AgendamentoException("Agendamento não encontrado.");

            var aval = new Avaliacao(tenantId, agendamentoId, ag.R_CliId);
            await _avaliacoes.CreateAsync(aval);
            await _uow.SaveChangesAsync();
            return aval.AvaToken;
        }

        public async Task<AvaliacaoViewModel> BuscarPorTokenAsync(Guid token)
        {
            var aval = await _avaliacoes.GetByTokenAsync(token);
            return aval == null ? null : ToViewModel(aval);
        }

        public async Task<AvaliacaoViewModel> ResponderAsync(Guid token, ResponderAvaliacaoInputModel input)
        {
            var aval = await _avaliacoes.GetByTokenAsync(token)
                ?? throw new DomainException("Token de avaliação inválido.");
            aval.Responder(input.Nota, input.Comentario);
            await _avaliacoes.UpdateAsync(aval);
            await _uow.SaveChangesAsync();
            return ToViewModel(aval);
        }

        public async Task<(IEnumerable<AvaliacaoViewModel> Items, int Total)> ListarAsync(
            int tenantId, int page, int pageSize, bool somenteRespondidas)
        {
            var (items, total) = await _avaliacoes.GetPagedAsync(tenantId, page, pageSize, somenteRespondidas);
            return (items.Select(ToViewModel), total);
        }

        public async Task<ResumoAvaliacoesViewModel> ResumoAsync(int tenantId, int top = 5)
        {
            var (media, total) = await _avaliacoes.CalcularResumoAsync(tenantId);
            var recentes = await _avaliacoes.GetPublicasAsync(tenantId, top);
            return new ResumoAvaliacoesViewModel
            {
                Media = Math.Round(media, 2),
                Total = total,
                Recentes = recentes.Select(a => new AvaliacaoPublicaViewModel
                {
                    ClienteNome = a.Cliente?.CliNome ?? "Cliente",
                    Nota = a.AvaNota ?? 0,
                    Comentario = a.AvaComentario,
                    RespondidoEm = a.AvaRespondidoEm ?? a.AvaCriadoEm
                }).ToList()
            };
        }

        public async Task AlterarVisibilidadeAsync(int tenantId, int avaliacaoId, bool publica)
        {
            var aval = await _avaliacoes.GetByIdAsync(avaliacaoId, tenantId)
                ?? throw new DomainException("Avaliação não encontrada.");
            aval.DefinirVisibilidade(publica);
            await _avaliacoes.UpdateAsync(aval);
            await _uow.SaveChangesAsync();
        }

        private static AvaliacaoViewModel ToViewModel(Avaliacao a) => new()
        {
            Id = a.AvaId,
            AgendamentoId = a.R_AgeId,
            Token = a.AvaToken,
            ClienteNome = a.Cliente?.CliNome,
            Nota = a.AvaNota,
            Comentario = a.AvaComentario,
            CriadoEm = a.AvaCriadoEm,
            RespondidoEm = a.AvaRespondidoEm,
            Publica = a.AvaPublica
        };
    }
}

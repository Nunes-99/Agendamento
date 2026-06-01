using AgendamentoPro.Application.Interfaces.Assinaturas;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Database.Repositories;

namespace AgendamentoPro.Application.UseCases.Assinaturas
{
    public class PlanoLimiteService : IPlanoLimiteService
    {
        private readonly IAssinaturaRepository _assinaturas;
        private readonly IRecursoRepository _recursos;

        public PlanoLimiteService(IAssinaturaRepository assinaturas, IRecursoRepository recursos)
        {
            _assinaturas = assinaturas;
            _recursos = recursos;
        }

        public async Task GarantirPodeCadastrarProfissionalAsync(int tenantId)
        {
            var ass = await _assinaturas.GetByTenantAsync(tenantId);
            // Sem assinatura ativa: deixa passar (tenant em onboarding ou super-admin).
            // O AssinaturaGuardMiddleware já bloqueia escrita em status inadimplentes.
            if (ass?.Plano == null) return;

            var ativos = (await _recursos.GetByTenantAsync(tenantId, somenteAtivos: true)).Count();
            if (!ass.Plano.RespeitaLimiteProfissionais(ativos))
            {
                throw new LimiteDoPlanoException(
                    "profissionais",
                    ass.Plano.PlnLimiteProfissionais,
                    $"Seu plano '{ass.Plano.PlnNome}' permite até {ass.Plano.PlnLimiteProfissionais} profissionais. " +
                    "Faça upgrade para cadastrar mais.");
            }
        }
    }
}

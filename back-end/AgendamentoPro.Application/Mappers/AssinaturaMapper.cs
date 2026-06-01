using AgendamentoPro.Application.ViewModels.Assinaturas;
using AgendamentoPro.Core.Entities.Assinaturas;

namespace AgendamentoPro.Application.Mappers
{
    public static class AssinaturaMapper
    {
        public static PlanoViewModel ToViewModel(Plano p) => new()
        {
            Id = p.PlnId,
            Nome = p.PlnNome,
            Descricao = p.PlnDescricao,
            Preco = p.PlnPreco,
            LimiteUnidades = p.PlnLimiteUnidades,
            LimiteProfissionais = p.PlnLimiteProfissionais,
            LimiteAgendamentosMes = p.PlnLimiteAgendamentosMes
        };

        public static AssinaturaViewModel ToViewModel(Assinatura a,
            IEnumerable<FaturaAssinatura> faturas = null, string checkoutUrl = null)
            => new()
            {
                Id = a.AssId,
                PlanoId = a.R_PlnId,
                PlanoNome = a.Plano?.PlnNome,
                PlanoPreco = a.Plano?.PlnPreco ?? 0,
                Status = a.AssStatus,
                StatusTexto = a.AssStatus.ToString(),
                Gateway = a.AssGateway,
                DataInicio = a.AssDataInicio,
                TrialAteEm = a.AssTrialAteEm,
                ProximoVencimento = a.AssProximoVencimento,
                UltimoPagamentoEm = a.AssUltimoPagamentoEm,
                AtrasoDesde = a.AssAtrasoDesde,
                ReadOnlyDesde = a.AssReadOnlyDesde,
                CanceladaEm = a.AssCanceladaEm,
                PermiteEscrita = a.PermiteEscrita(),
                CheckoutUrl = checkoutUrl,
                Faturas = (faturas ?? Enumerable.Empty<FaturaAssinatura>())
                    .Select(ToViewModel).ToList()
            };

        public static FaturaAssinaturaViewModel ToViewModel(FaturaAssinatura f) => new()
        {
            Id = f.FasId,
            Valor = f.FasValor,
            Status = f.FasStatus,
            StatusTexto = f.FasStatus.ToString(),
            ReferenciaInicio = f.FasReferenciaInicio,
            ReferenciaFim = f.FasReferenciaFim,
            VencimentoEm = f.FasVencimentoEm,
            PagoEm = f.FasPagoEm
        };
    }
}

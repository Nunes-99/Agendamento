using AgendamentoPro.Core.Entities.Common;
using AgendamentoPro.Core.Entities.Tenants;
using AgendamentoPro.Core.Exceptions;
using AgendamentoPro.Core.Interfaces.Common;

namespace AgendamentoPro.Core.Entities.Servicos
{
    public enum TipoDesconto
    {
        Percentual = 1,
        ValorFixo = 2
    }

    /// <summary>
    /// Cupom de desconto aplicável em agendamentos. Modelo simples:
    /// - código único por tenant
    /// - tipo (percentual/valor fixo)
    /// - validade
    /// - limite de usos (total e por cliente)
    /// </summary>
    public class Cupom : SoftDeletableEntity, ITenantScoped
    {
        public int CupId { get; private set; }
        public int R_TenId { get; private set; }
        public string CupCodigo { get; private set; }
        public TipoDesconto CupTipo { get; private set; }
        public decimal CupValor { get; private set; }
        public DateTime CupValidoDe { get; private set; }
        public DateTime CupValidoAte { get; private set; }
        public int CupUsosMaximos { get; private set; }
        public int CupUsosFeitos { get; private set; }
        public bool CupAtivo { get; private set; }
        public DateTime CupCriadoEm { get; private set; }

        public Tenant Tenant { get; private set; }

        protected Cupom() { }

        public Cupom(int rTenId, string codigo, TipoDesconto tipo, decimal valor,
            DateTime validoDe, DateTime validoAte, int usosMaximos)
        {
            if (rTenId <= 0) throw new DomainException("Tenant é obrigatório.");
            if (string.IsNullOrWhiteSpace(codigo)) throw new DomainException("Código do cupom obrigatório.");
            if (valor <= 0) throw new DomainException("Valor do desconto deve ser positivo.");
            if (tipo == TipoDesconto.Percentual && valor > 100)
                throw new DomainException("Desconto percentual máximo: 100.");
            if (validoAte <= validoDe) throw new DomainException("Data fim deve ser posterior à data início.");

            R_TenId = rTenId;
            CupCodigo = codigo.Trim().ToUpperInvariant();
            CupTipo = tipo;
            CupValor = valor;
            CupValidoDe = validoDe;
            CupValidoAte = validoAte;
            CupUsosMaximos = usosMaximos > 0 ? usosMaximos : int.MaxValue;
            CupUsosFeitos = 0;
            CupAtivo = true;
            CupCriadoEm = DateTime.UtcNow;
        }

        public bool EhValido(DateTime agora) =>
            CupAtivo && agora >= CupValidoDe && agora <= CupValidoAte && CupUsosFeitos < CupUsosMaximos;

        /// <summary>Aplica o desconto ao valor base; retorna o novo valor (não pode ficar negativo).</summary>
        public decimal CalcularDesconto(decimal valorBase) => CupTipo switch
        {
            TipoDesconto.Percentual => Math.Max(0, Math.Round(valorBase * (1 - CupValor / 100m), 2)),
            TipoDesconto.ValorFixo => Math.Max(0, valorBase - CupValor),
            _ => valorBase
        };

        public void RegistrarUso() => CupUsosFeitos++;
        public void Desativar() => CupAtivo = false;
        public void Ativar() => CupAtivo = true;
    }
}

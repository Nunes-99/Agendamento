using AgendamentoPro.Application.InputModels.Servicos;
using FluentValidation;

namespace AgendamentoPro.Application.Validators.Servicos
{
    public class ComboValidator : AbstractValidator<ComboInputModel>
    {
        public ComboValidator()
        {
            RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Descricao).MaximumLength(1000);
            RuleFor(x => x.ImagemUrl).MaximumLength(500);
            RuleFor(x => x.PrecoPromocional)
                .GreaterThan(0).WithMessage("Preço promocional deve ser maior que zero.")
                .LessThan(1_000_000m);
            RuleFor(x => x.Ordem).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ServicoIds)
                .NotEmpty().WithMessage("Combo deve ter pelo menos 1 serviço.")
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("ServicoIds não pode ter duplicatas.");
        }
    }
}

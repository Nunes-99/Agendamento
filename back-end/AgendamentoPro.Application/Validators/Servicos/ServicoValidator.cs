using AgendamentoPro.Application.InputModels.Servicos;
using FluentValidation;

namespace AgendamentoPro.Application.Validators.Servicos
{
    public class ServicoValidator : AbstractValidator<ServicoInputModel>
    {
        public ServicoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(150);
            RuleFor(x => x.Descricao).MaximumLength(1000);
            RuleFor(x => x.Categoria).MaximumLength(100);
            RuleFor(x => x.ImagemUrl).MaximumLength(500);
            RuleFor(x => x.Preco)
                .GreaterThan(0).WithMessage("Preço deve ser maior que zero.")
                .LessThan(1_000_000m);
            RuleFor(x => x.DuracaoMinutos)
                .GreaterThan(0).WithMessage("Duração deve ser maior que zero.")
                .LessThanOrEqualTo(24 * 60).WithMessage("Duração não pode exceder 24h.");
            RuleFor(x => x.Ordem).GreaterThanOrEqualTo(0);
        }
    }
}

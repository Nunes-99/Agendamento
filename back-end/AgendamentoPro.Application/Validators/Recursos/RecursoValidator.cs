using AgendamentoPro.Application.InputModels.Recursos;
using FluentValidation;

namespace AgendamentoPro.Application.Validators.Recursos
{
    public class RecursoValidator : AbstractValidator<RecursoInputModel>
    {
        public RecursoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(150);
            RuleFor(x => x.Descricao).MaximumLength(500);
            RuleFor(x => x.Tipo).MaximumLength(50);
            RuleFor(x => x.ImagemUrl).MaximumLength(500);
            RuleFor(x => x.Ordem).GreaterThanOrEqualTo(0);
        }
    }
}

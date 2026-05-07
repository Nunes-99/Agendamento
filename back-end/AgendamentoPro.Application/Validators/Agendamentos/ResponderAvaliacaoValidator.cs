using AgendamentoPro.Application.InputModels.Agendamentos;
using FluentValidation;

namespace AgendamentoPro.Application.Validators.Agendamentos
{
    public class ResponderAvaliacaoValidator : AbstractValidator<ResponderAvaliacaoInputModel>
    {
        public ResponderAvaliacaoValidator()
        {
            RuleFor(x => x.Nota).InclusiveBetween(1, 5).WithMessage("Nota deve ser entre 1 e 5.");
            RuleFor(x => x.Comentario).MaximumLength(1000);
        }
    }
}

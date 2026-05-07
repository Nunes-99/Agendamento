using AgendamentoPro.Application.InputModels.Servicos;
using AgendamentoPro.Application.Validators.Agendamentos;
using FluentValidation;

namespace AgendamentoPro.Application.Validators.Servicos
{
    public class AgendarComboValidator : AbstractValidator<AgendarComboInputModel>
    {
        public AgendarComboValidator()
        {
            RuleFor(x => x.Data)
                .NotEmpty()
                .Must(d => d.Date >= DateTime.UtcNow.Date)
                .WithMessage("Data não pode ser no passado.");
            RuleFor(x => x.HoraInicio)
                .Must(h => h >= TimeSpan.Zero && h < TimeSpan.FromDays(1))
                .WithMessage("Hora de início inválida.");
            RuleFor(x => x.Observacao).MaximumLength(1000);
            RuleFor(x => x.Cliente).NotNull().WithMessage("Dados do cliente são obrigatórios.");
            RuleFor(x => x.Cliente).SetValidator(new ClientePublicoValidator()).When(x => x.Cliente != null);
        }
    }
}

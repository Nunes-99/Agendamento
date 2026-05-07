using AgendamentoPro.Application.InputModels.Auth;
using FluentValidation;

namespace AgendamentoPro.Application.Validators.Auth
{
    public class SolicitarOtpValidator : AbstractValidator<SolicitarOtpInputModel>
    {
        public SolicitarOtpValidator()
        {
            RuleFor(x => x.Telefone)
                .NotEmpty().WithMessage("Telefone é obrigatório.")
                .Matches(@"^\D*\d{10,13}\D*$")
                .WithMessage("Telefone deve ter 10 a 13 dígitos (DDD + número, com ou sem 9 inicial).");
        }
    }

    public class ValidarOtpValidator : AbstractValidator<ValidarOtpInputModel>
    {
        public ValidarOtpValidator()
        {
            RuleFor(x => x.Telefone).NotEmpty().Matches(@"^\D*\d{10,13}\D*$");
            RuleFor(x => x.Codigo)
                .NotEmpty().WithMessage("Código é obrigatório.")
                .Length(6).WithMessage("Código deve ter exatamente 6 dígitos.")
                .Matches(@"^\d{6}$").WithMessage("Código deve conter apenas dígitos.");
        }
    }
}

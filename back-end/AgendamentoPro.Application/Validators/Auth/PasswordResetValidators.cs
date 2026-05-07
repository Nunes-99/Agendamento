using AgendamentoPro.Application.InputModels.Auth;
using FluentValidation;

namespace AgendamentoPro.Application.Validators.Auth
{
    public class SolicitarResetSenhaValidator : AbstractValidator<SolicitarResetSenhaInputModel>
    {
        public SolicitarResetSenhaValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail é obrigatório.")
                .EmailAddress().WithMessage("E-mail inválido.")
                .MaximumLength(255);
        }
    }

    public class RedefinirSenhaValidator : AbstractValidator<RedefinirSenhaInputModel>
    {
        public RedefinirSenhaValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token é obrigatório.")
                .MaximumLength(200);
            RuleFor(x => x.NovaSenha)
                .NotEmpty()
                .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres.")
                .MaximumLength(200)
                .Matches(@"[A-Z]").WithMessage("Senha deve conter ao menos uma letra maiúscula.")
                .Matches(@"[a-z]").WithMessage("Senha deve conter ao menos uma letra minúscula.")
                .Matches(@"\d").WithMessage("Senha deve conter ao menos um dígito.");
        }
    }
}

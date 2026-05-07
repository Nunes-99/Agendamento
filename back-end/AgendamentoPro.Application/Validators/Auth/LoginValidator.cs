using AgendamentoPro.Application.InputModels.Auth;
using FluentValidation;

namespace AgendamentoPro.Application.Validators.Auth
{
    public class LoginValidator : AbstractValidator<LoginInputModel>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail é obrigatório.")
                .EmailAddress().WithMessage("E-mail em formato inválido.")
                .MaximumLength(255);
            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("Senha é obrigatória.")
                .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres.")
                .MaximumLength(200);
            RuleFor(x => x.TenantSlug)
                .MaximumLength(80)
                .Matches("^[a-z0-9-]*$").WithMessage("Slug deve conter apenas minúsculas, números e hífens.")
                .When(x => !string.IsNullOrEmpty(x.TenantSlug));
        }
    }

    public class RefreshTokenValidator : AbstractValidator<RefreshTokenInputModel>
    {
        public RefreshTokenValidator()
        {
            RuleFor(x => x.AccessToken).NotEmpty().WithMessage("Access token é obrigatório.");
            RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token é obrigatório.");
        }
    }
}

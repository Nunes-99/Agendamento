using AgendamentoPro.Application.InputModels.Clientes;
using FluentValidation;

namespace AgendamentoPro.Application.Validators.Clientes
{
    public class ClienteValidator : AbstractValidator<ClienteInputModel>
    {
        public ClienteValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(200);
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("E-mail inválido.")
                .MaximumLength(255)
                .When(x => !string.IsNullOrEmpty(x.Email));
            RuleFor(x => x.Telefone).MaximumLength(30);
            RuleFor(x => x.WhatsApp).MaximumLength(30);
            RuleFor(x => x.Cpf).MaximumLength(14);
            RuleFor(x => x.Observacao).MaximumLength(1000);
            RuleFor(x => x)
                .Must(c => !string.IsNullOrEmpty(c.Telefone) || !string.IsNullOrEmpty(c.WhatsApp) || !string.IsNullOrEmpty(c.Email))
                .WithMessage("Informe ao menos um meio de contato.");
        }
    }
}

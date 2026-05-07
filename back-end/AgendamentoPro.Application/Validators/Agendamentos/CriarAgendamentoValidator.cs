using AgendamentoPro.Application.InputModels.Agendamentos;
using FluentValidation;

namespace AgendamentoPro.Application.Validators.Agendamentos
{
    public class ClientePublicoValidator : AbstractValidator<ClientePublicoInputModel>
    {
        public ClientePublicoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome do cliente é obrigatório.")
                .MaximumLength(200);
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("E-mail inválido.")
                .MaximumLength(255)
                .When(x => !string.IsNullOrEmpty(x.Email));
            RuleFor(x => x.Telefone)
                .MaximumLength(30);
            RuleFor(x => x.WhatsApp)
                .MaximumLength(30);
            RuleFor(x => x.Cpf)
                .MaximumLength(14);
            RuleFor(x => x)
                .Must(c => !string.IsNullOrEmpty(c.Telefone) || !string.IsNullOrEmpty(c.WhatsApp) || !string.IsNullOrEmpty(c.Email))
                .WithMessage("Informe ao menos um meio de contato (telefone, WhatsApp ou e-mail).");
        }
    }

    public class CriarAgendamentoValidator : AbstractValidator<CriarAgendamentoInputModel>
    {
        public CriarAgendamentoValidator()
        {
            RuleFor(x => x.ServicoId).GreaterThan(0).WithMessage("ServicoId é obrigatório.");
            RuleFor(x => x.Data)
                .NotEmpty().WithMessage("Data é obrigatória.")
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

    public class ReagendarValidator : AbstractValidator<ReagendarInputModel>
    {
        public ReagendarValidator()
        {
            RuleFor(x => x.NovaData)
                .NotEmpty()
                .Must(d => d.Date >= DateTime.UtcNow.Date)
                .WithMessage("Nova data não pode ser no passado.");
            RuleFor(x => x.NovaHoraInicio)
                .Must(h => h >= TimeSpan.Zero && h < TimeSpan.FromDays(1))
                .WithMessage("Hora de início inválida.");
        }
    }

    public class CancelarAgendamentoValidator : AbstractValidator<CancelarAgendamentoInputModel>
    {
        public CancelarAgendamentoValidator()
        {
            RuleFor(x => x.Motivo).MaximumLength(500);
        }
    }

    public class CriarAgendamentoAdminValidator : AbstractValidator<CriarAgendamentoAdminInputModel>
    {
        public CriarAgendamentoAdminValidator()
        {
            RuleFor(x => x.ServicoId).GreaterThan(0);
            RuleFor(x => x.Data).NotEmpty();
            RuleFor(x => x.HoraInicio)
                .Must(h => h >= TimeSpan.Zero && h < TimeSpan.FromDays(1));
            RuleFor(x => x.Observacao).MaximumLength(1000);
            RuleFor(x => x.Valor).GreaterThanOrEqualTo(0).When(x => x.Valor.HasValue);
            RuleFor(x => x)
                .Must(x => x.ClienteId.HasValue || x.Cliente != null)
                .WithMessage("Informe ClienteId ou os dados do cliente.");
            RuleFor(x => x.Cliente).SetValidator(new ClientePublicoValidator()).When(x => x.Cliente != null);
        }
    }
}

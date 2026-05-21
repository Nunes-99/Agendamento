using AgendamentoPro.Application.InputModels.Tenants;
using FluentValidation;
using System.Text.RegularExpressions;

namespace AgendamentoPro.Application.Validators.Tenants
{
    public class CriarTenantValidator : AbstractValidator<CriarTenantInputModel>
    {
        private static readonly Regex SlugRegex = new("^[a-z0-9](?:[a-z0-9-]{0,78}[a-z0-9])?$", RegexOptions.Compiled);

        public CriarTenantValidator()
        {
            RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Slug é obrigatório.")
                .MaximumLength(80)
                .Must(s => SlugRegex.IsMatch(s ?? string.Empty))
                .WithMessage("Slug deve conter apenas minúsculas, números e hífens (3 a 80 caracteres, não pode iniciar/terminar com hífen).");
            RuleFor(x => x.Segmento).MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
            RuleFor(x => x.Telefone).MaximumLength(30);
            RuleFor(x => x.AdminNome).NotEmpty().MaximumLength(200);
            RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(255);
            RuleFor(x => x.AdminSenha)
                .NotEmpty().WithMessage("Senha do admin é obrigatória.")
                .MinimumLength(8).WithMessage("Senha do admin deve ter no mínimo 8 caracteres.")
                .MaximumLength(200);
        }
    }

    public class AtualizarTenantValidator : AbstractValidator<AtualizarTenantInputModel>
    {
        public AtualizarTenantValidator()
        {
            RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Email).EmailAddress().MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Email));
            RuleFor(x => x.Cnpj).MaximumLength(18);
            RuleFor(x => x.Telefone).MaximumLength(30);
            RuleFor(x => x.WhatsApp).MaximumLength(30);
            RuleFor(x => x.Endereco).MaximumLength(255);
            RuleFor(x => x.Cidade).MaximumLength(100);
            RuleFor(x => x.Estado).MaximumLength(2);
            RuleFor(x => x.Cep).MaximumLength(10);
            RuleFor(x => x.Descricao).MaximumLength(2000);
        }
    }

    public class AtualizarPersonalizacaoValidator : AbstractValidator<AtualizarPersonalizacaoInputModel>
    {
        // CSS hex aceita só 3, 4 (com alpha curto), 6 ou 8 (com alpha) caracteres.
        // Antes o regex "{3,8}" deixava passar #FFFF (alpha curto - ok), #FFFFF (5
        // - inválido em CSS) e #FFFFFFF (7 - inválido). Frontend renderizaria preto
        // ou transparente nas cores inválidas.
        private static readonly Regex CorRegex = new(
            "^#([0-9a-fA-F]{3}|[0-9a-fA-F]{4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
            RegexOptions.Compiled);

        /// <summary>
        /// Aceita URL absoluta http/https ou caminho relativo (ex: /uploads/...).
        /// Rejeita schemes perigosos (javascript:, data:, file:, vbscript:) que
        /// poderiam virar XSS quando colados em href/src do frontend.
        /// </summary>
        private static bool UrlSegura(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return true; // opcional
            var trimmed = url.Trim();
            if (trimmed.StartsWith("/")) return true; // caminho relativo do próprio host
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        public AtualizarPersonalizacaoValidator()
        {
            RuleFor(x => x.LogoUrl).MaximumLength(500)
                .Must(UrlSegura).WithMessage("LogoUrl deve ser http(s) ou caminho relativo.");
            RuleFor(x => x.BannerUrl).MaximumLength(500)
                .Must(UrlSegura).WithMessage("BannerUrl deve ser http(s) ou caminho relativo.");
            RuleFor(x => x.FaviconUrl).MaximumLength(500)
                .Must(UrlSegura).WithMessage("FaviconUrl deve ser http(s) ou caminho relativo.");
            RuleFor(x => x.Fonte).MaximumLength(50);
            RuleFor(x => x.CorPrimaria).Must(c => string.IsNullOrEmpty(c) || CorRegex.IsMatch(c))
                .WithMessage("Cor primária em formato HEX inválido.");
            RuleFor(x => x.CorSecundaria).Must(c => string.IsNullOrEmpty(c) || CorRegex.IsMatch(c))
                .WithMessage("Cor secundária em formato HEX inválido.");
            RuleFor(x => x.CorAcento).Must(c => string.IsNullOrEmpty(c) || CorRegex.IsMatch(c))
                .WithMessage("Cor de acento em formato HEX inválido.");
        }
    }

    public class AtualizarRegrasNegocioValidator : AbstractValidator<AtualizarRegrasNegocioInputModel>
    {
        public AtualizarRegrasNegocioValidator()
        {
            RuleFor(x => x.PercentualEntrada)
                .InclusiveBetween(0m, 100m).WithMessage("Percentual de entrada deve estar entre 0 e 100.");
            RuleFor(x => x.BufferMinutos).InclusiveBetween(0, 240);
            RuleFor(x => x.AntecedenciaMinHoras).InclusiveBetween(0, 24 * 30);
            RuleFor(x => x.AntecedenciaMaxDias).InclusiveBetween(1, 365);
            RuleFor(x => x.LimiteCancelamentoHoras).InclusiveBetween(0, 24 * 30);
        }
    }
}

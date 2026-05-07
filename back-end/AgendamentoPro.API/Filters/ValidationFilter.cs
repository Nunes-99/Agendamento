using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgendamentoPro.API.Filters
{
    /// <summary>
    /// Action filter que executa o validator FluentValidation registrado para o tipo
    /// de cada parâmetro [FromBody] do endpoint. Retorna 400 com lista de erros se inválido.
    /// </summary>
    public class FluentValidationFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _services;
        public FluentValidationFilter(IServiceProvider services) { _services = services; }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var arg in context.ActionArguments.Values.Where(v => v != null))
            {
                var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
                var validator = _services.GetService(validatorType) as IValidator;
                if (validator == null) continue;

                var ctx = new ValidationContext<object>(arg);
                var result = await validator.ValidateAsync(ctx);
                if (!result.IsValid)
                {
                    var problem = new ValidationProblemDetails(
                        result.Errors
                            .GroupBy(e => e.PropertyName ?? string.Empty)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
                    {
                        Title = "Erro de validação",
                        Status = StatusCodes.Status400BadRequest
                    };
                    context.Result = new BadRequestObjectResult(problem);
                    return;
                }
            }
            await next();
        }
    }
}

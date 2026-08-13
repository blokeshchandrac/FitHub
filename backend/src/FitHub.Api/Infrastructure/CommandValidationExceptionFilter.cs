using FitHub.Application.Common;
using FitHub.Application.Equipment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FitHub.Api.Infrastructure;

public sealed class CommandValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        context.Result = context.Exception switch
        {
            CommandValidationException exception => new BadRequestObjectResult(new ValidationProblemDetails(exception.Errors.ToDictionary(item => item.Key, item => item.Value))),
            EquipmentConcurrencyException exception => new ConflictObjectResult(new ProblemDetails { Status = 409, Title = "Equipment changed", Detail = exception.Message }),
            UnauthorizedAccessException => new ObjectResult(new ProblemDetails { Status = 403, Title = "Action not allowed", Detail = "You are not permitted to perform this action." }) { StatusCode = 403 },
            ArgumentException exception => new BadRequestObjectResult(new ProblemDetails { Status = 400, Title = "Invalid request", Detail = exception.Message }),
            InvalidOperationException exception => new ConflictObjectResult(new ProblemDetails { Status = 409, Title = "Invalid state transition", Detail = exception.Message }),
            KeyNotFoundException exception => new NotFoundObjectResult(new ProblemDetails { Status = 404, Title = "Not found", Detail = exception.Message }),
            _ => null
        };
        if (context.Result is null) return;
        context.ExceptionHandled = true;
    }
}

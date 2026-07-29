using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SuperShop.Domain.Exceptions;

namespace SuperShop.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var problem = exception switch
        {
            ValidationException validation => ValidationProblem(validation),
            UnauthorizedException => Problem(StatusCodes.Status401Unauthorized, "Não autenticado", exception.Message),
            ForbiddenException => Problem(StatusCodes.Status403Forbidden, "Sem permissão", exception.Message),
            NotFoundException => Problem(StatusCodes.Status404NotFound, "Não encontrado", exception.Message),
            ConflictException => Problem(StatusCodes.Status409Conflict, "Conflito", exception.Message),
            InsufficientStockException => Problem(StatusCodes.Status422UnprocessableEntity, "Stock insuficiente", exception.Message),
            _ => Problem(StatusCodes.Status500InternalServerError, "Erro interno",
                "Ocorreu um erro inesperado. Cite o identificador abaixo no pedido de suporte.")
        };

        problem.Extensions["traceId"] = traceId;

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (environment.IsDevelopment())
            {
                problem.Extensions["exception"] = exception.ToString();
            }
        }
        else
        {
            logger.LogWarning("{Status} on {Method} {Path}: {Message}",
                problem.Status, context.Request.Method, context.Request.Path, exception.Message);
        }

        context.Response.Clear();
        context.Response.StatusCode = problem.Status!.Value;

        await context.Response.WriteAsJsonAsync(
            problem, problem.GetType(), options: null, contentType: "application/problem+json");
    }

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://developer.mozilla.org/docs/Web/HTTP/Status/{status}"
        };

    private static ValidationProblemDetails ValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => char.ToLowerInvariant(g.Key[0]) + g.Key[1..],
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Erro de validação",
            Type = "https://developer.mozilla.org/docs/Web/HTTP/Status/400"
        };
    }
}

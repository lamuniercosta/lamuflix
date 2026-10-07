using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LamuFlix.Api.ExceptionHandling;

public sealed class ValidationExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);
        cancellationToken.ThrowIfCancellationRequested();

        return exception switch
        {
            ValidationException validationException => await WriteAsync(
                httpContext,
                StatusCodes.Status422UnprocessableEntity,
                CreateValidationProblem(validationException)),
            BadHttpRequestException => await WriteAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                CreateBadRequestProblem()),
            NotFoundException => await WriteAsync(
                httpContext,
                StatusCodes.Status404NotFound,
                CreateNotFoundProblem()),
            InvalidTransitionException => await WriteAsync(
                httpContext,
                StatusCodes.Status409Conflict,
                CreateConflictProblem()),
            FeatureDisabledException => await WriteAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                CreateForbiddenProblem()),
            _ => false,
        };
    }

    private async ValueTask<bool> WriteAsync(HttpContext httpContext, int statusCode, ProblemDetails problem)
    {
        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        });
    }

    private static ProblemDetails CreateValidationProblem(ValidationException validationException)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Unprocessable Entity",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.21",
        };
        problem.Extensions["errors"] = validationException.Errors;
        return problem;
    }

    private static ProblemDetails CreateBadRequestProblem() =>
        new()
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bad Request",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        };

    private static ProblemDetails CreateNotFoundProblem() =>
        new()
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not Found",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        };

    private static ProblemDetails CreateConflictProblem() =>
        new()
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        };

    private static ProblemDetails CreateForbiddenProblem() =>
        new()
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        };
}

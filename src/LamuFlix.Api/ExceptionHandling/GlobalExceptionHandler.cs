using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LamuFlix.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private const string ReasonExtensionName = "reason";

    private const string QuotaExtensionName = "quota";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (TryMapSubtitleProblem(exception, httpContext, out var subtitleStatus, out var subtitleProblem))
        {
            return await WriteAsync(httpContext, subtitleStatus, subtitleProblem);
        }

        var (status, problem) = MapCoreProblem(exception);
        return await WriteAsync(httpContext, status, problem);
    }

    public static bool ShouldSuppressDiagnostics(Exception exception) => exception switch
    {
        ValidationException => true,
        BadHttpRequestException => true,
        NotFoundException => true,
        InvalidTransitionException => true,
        ImdbIdConflictException => true,
        FeatureDisabledException => true,
        LibraryFileAccessDeniedException => true,
        SubtitleQuotaExceededException => true,
        SubtitleAcquisitionUnavailableException => true,
        SubtitleProviderNotConfiguredException => true,
        SubtitleRequestRejectedException => true,
        SubtitleMovieNotFoundException => true,
        SubtitleSearchRateLimitedException => true,
        SubtitleStaleCandidateException => true,
        SubtitleReadOnlyLibraryException => true,
        SubtitleDownloadQuotaExhaustedException => true,
        _ => false,
    };

    private static bool TryMapSubtitleProblem(
        Exception exception,
        HttpContext httpContext,
        out int status,
        out ProblemDetails problem)
    {
        switch (exception)
        {
            case SubtitleQuotaExceededException quotaExceeded:
                status = StatusCodes.Status429TooManyRequests;
                problem = CreateQuotaProblem(quotaExceeded, httpContext);
                return true;
            case SubtitleDownloadQuotaExhaustedException downloadQuotaExhausted:
                status = StatusCodes.Status429TooManyRequests;
                problem = CreateDownloadQuotaProblem(downloadQuotaExhausted);
                return true;
            case SubtitleStaleCandidateException:
                status = StatusCodes.Status400BadRequest;
                problem = WithReason(CreateBadRequestProblem(), SubtitleProblemReasons.StaleCandidate);
                return true;
            case SubtitleReadOnlyLibraryException:
                status = StatusCodes.Status409Conflict;
                problem = WithReason(CreateConflictProblem(), SubtitleProblemReasons.ReadOnlyLibrary);
                return true;
            case SubtitleSearchRateLimitedException searchRateLimited:
                status = StatusCodes.Status429TooManyRequests;
                problem = CreateSearchRateLimitedProblem(searchRateLimited, httpContext);
                return true;
            case SubtitleProviderTimeoutException:
                status = StatusCodes.Status504GatewayTimeout;
                problem = WithReason(CreateGatewayTimeoutProblem(), SubtitleProblemReasons.ProviderTimeout);
                return true;
            case SubtitleProviderFailureException:
                status = StatusCodes.Status502BadGateway;
                problem = WithReason(CreateBadGatewayProblem(), SubtitleProblemReasons.ProviderUnavailable);
                return true;
            case SubtitleProviderNotConfiguredException:
                status = StatusCodes.Status409Conflict;
                problem = WithReason(CreateConflictProblem(), SubtitleProblemReasons.ProviderNotConfigured);
                return true;
            case SubtitleAcquisitionUnavailableException:
                status = StatusCodes.Status409Conflict;
                problem = CreateConflictProblem();
                return true;
            case SubtitleMovieNotFoundException:
                status = StatusCodes.Status404NotFound;
                problem = WithReason(CreateNotFoundProblem(), SubtitleProblemReasons.MovieNotFound);
                return true;
            case SubtitleRequestRejectedException:
                status = StatusCodes.Status400BadRequest;
                problem = WithReason(CreateBadRequestProblem(), SubtitleProblemReasons.InvalidRequest);
                return true;
            default:
                status = 0;
                // ReSharper disable once NullableWarningSuppressionIsUsed // out value unread when returning false
                problem = null!;
                return false;
        }
    }

    private static (int Status, ProblemDetails Problem) MapCoreProblem(Exception exception)
    {
        if (TryMapRequestProblem(exception, out var status, out var problem))
        {
            return (status, problem);
        }

        return MapAccessProblem(exception);
    }

    private static bool TryMapRequestProblem(Exception exception, out int status, out ProblemDetails problem)
    {
        switch (exception)
        {
            case ValidationException validationException:
                status = StatusCodes.Status422UnprocessableEntity;
                problem = CreateValidationProblem(validationException);
                return true;
            case BadHttpRequestException:
                status = StatusCodes.Status400BadRequest;
                problem = CreateBadRequestProblem();
                return true;
            case NotFoundException:
                status = StatusCodes.Status404NotFound;
                problem = CreateNotFoundProblem();
                return true;
            case InvalidTransitionException:
                status = StatusCodes.Status409Conflict;
                problem = CreateConflictProblem();
                return true;
            case ImdbIdConflictException imdbIdConflict:
                status = StatusCodes.Status409Conflict;
                problem = CreateImdbIdConflictProblem(imdbIdConflict);
                return true;
            default:
                status = 0;
                // ReSharper disable once NullableWarningSuppressionIsUsed // out value unread when returning false
                problem = null!;
                return false;
        }
    }

    private static (int Status, ProblemDetails Problem) MapAccessProblem(Exception exception)
    {
        return exception switch
        {
            FeatureDisabledException => (StatusCodes.Status403Forbidden, CreateForbiddenProblem()),
            LibraryFileAccessDeniedException => (StatusCodes.Status403Forbidden, CreateAccessDeniedProblem()),
            _ => (StatusCodes.Status500InternalServerError, CreateServerErrorProblem()),
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
        problem.Extensions[ReasonExtensionName] = SubtitleProblemReasons.InvalidRequest;
        problem.Extensions["errors"] = validationException.Errors;
        return problem;
    }

    private static ProblemDetails CreateBadRequestProblem() =>
        CreateProblem(
            StatusCodes.Status400BadRequest,
            "Bad Request",
            "https://tools.ietf.org/html/rfc9110#section-15.5.1");

    private static ProblemDetails CreateNotFoundProblem() =>
        CreateProblem(
            StatusCodes.Status404NotFound,
            "Not Found",
            "https://tools.ietf.org/html/rfc9110#section-15.5.5");

    private static ProblemDetails CreateConflictProblem() =>
        CreateProblem(
            StatusCodes.Status409Conflict,
            "Conflict",
            "https://tools.ietf.org/html/rfc9110#section-15.5.10");

    private static ProblemDetails CreateImdbIdConflictProblem(ImdbIdConflictException imdbIdConflict)
    {
        var problem = CreateConflictProblem();
        problem.Detail = imdbIdConflict.Message;
        problem.Extensions["heldByMovieId"] = imdbIdConflict.HeldBy.Value;
        return problem;
    }

    private static ProblemDetails CreateForbiddenProblem() =>
        CreateProblem(
            StatusCodes.Status403Forbidden,
            "Forbidden",
            "https://tools.ietf.org/html/rfc9110#section-15.5.4");

    private static ProblemDetails CreateAccessDeniedProblem() =>
        WithReason(CreateForbiddenProblem(), SubtitleProblemReasons.AccessDenied);

    private static ProblemDetails CreateQuotaProblem(
        SubtitleQuotaExceededException quotaExceeded,
        HttpContext httpContext)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.30",
        };
        problem.Extensions[ReasonExtensionName] = SubtitleProblemReasons.DownloadQuotaExhausted;

        if (quotaExceeded.RetryAfter is { } retryAfter)
        {
            httpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        return problem;
    }

    private static ProblemDetails CreateDownloadQuotaProblem(SubtitleDownloadQuotaExhaustedException quotaExhausted)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.30",
        };
        problem.Extensions[ReasonExtensionName] = SubtitleProblemReasons.DownloadQuotaExhausted;
        problem.Extensions[QuotaExtensionName] = QuotaExtension(quotaExhausted.Quota);
        return problem;
    }

    private static Dictionary<string, object?> QuotaExtension(SubtitleQuota quota) =>
        new(StringComparer.Ordinal)
        {
            ["remainingDownloads"] = quota.RemainingDownloads,
            ["resetAtUtc"] = quota.ResetAtUtc,
        };

    private static ProblemDetails CreateSearchRateLimitedProblem(
        SubtitleSearchRateLimitedException searchRateLimited,
        HttpContext httpContext)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.30",
        };
        problem.Extensions[ReasonExtensionName] = SubtitleProblemReasons.SearchRateLimited;

        if (searchRateLimited.RetryAfter is { } retryAfter)
        {
            httpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        return problem;
    }

    private static ProblemDetails WithReason(ProblemDetails problem, string reason)
    {
        problem.Extensions[ReasonExtensionName] = reason;
        return problem;
    }

    private static ProblemDetails CreateBadGatewayProblem() =>
        CreateProblem(
            StatusCodes.Status502BadGateway,
            "Bad Gateway",
            "https://tools.ietf.org/html/rfc9110#section-15.6.3");

    private static ProblemDetails CreateGatewayTimeoutProblem() =>
        CreateProblem(
            StatusCodes.Status504GatewayTimeout,
            "Gateway Timeout",
            "https://tools.ietf.org/html/rfc9110#section-15.6.5");

    private static ProblemDetails CreateServerErrorProblem() =>
        CreateProblem(
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "https://tools.ietf.org/html/rfc9110#section-15.6.1");

    private static ProblemDetails CreateProblem(int status, string title, string type) =>
        new()
        {
            Status = status,
            Title = title,
            Type = type,
        };
}

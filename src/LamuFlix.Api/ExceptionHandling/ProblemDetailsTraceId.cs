using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace LamuFlix.Api.ExceptionHandling;

/// <summary>
/// Makes the ProblemDetails <c>traceId</c> extension the bare W3C trace id.
/// ASP.NET Core fills it from <see cref="Activity.Id"/>, which for W3C activities is the whole
/// <c>traceparent</c> value (<c>00-{traceId}-{spanId}-{flags}</c>) and cannot be pasted into the
/// Aspire dashboard's <c>/traces/detail/{traceId}</c>.
/// </summary>
public static class ProblemDetailsTraceId
{
    public const string ExtensionName = "traceId";

    public static void Apply(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity ?? Activity.Current;
        context.ProblemDetails.Extensions[ExtensionName] = activity is { IdFormat: ActivityIdFormat.W3C }
            ? activity.TraceId.ToHexString()
            : context.HttpContext.TraceIdentifier;
    }
}

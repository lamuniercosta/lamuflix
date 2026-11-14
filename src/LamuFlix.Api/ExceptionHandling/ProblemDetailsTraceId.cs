using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace LamuFlix.Api.ExceptionHandling;

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

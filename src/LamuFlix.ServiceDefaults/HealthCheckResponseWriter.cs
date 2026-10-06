using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.ServiceDefaults;

internal static class HealthCheckResponseWriter
{
    internal static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        var checks = CreateChecks(report);
        if (report.Status == HealthStatus.Unhealthy)
        {
            return WriteUnhealthyAsync(context, checks);
        }

        return context.Response.WriteAsJsonAsync(new JsonObject
        {
            ["status"] = report.Status.ToString(),
            ["checks"] = checks,
        });
    }

    private static JsonArray CreateChecks(HealthReport report)
    {
        var checks = new JsonArray();
        foreach (var entry in report.Entries)
        {
            checks.Add(new JsonObject
            {
                ["name"] = entry.Key,
                ["status"] = entry.Value.Status.ToString(),
                ["durationMs"] = (int)entry.Value.Duration.TotalMilliseconds,
            });
        }

        return checks;
    }

    private static Task WriteUnhealthyAsync(HttpContext context, JsonArray checks)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        var problemDetails = new ProblemDetails
        {
            Title = "Service Unavailable",
            Status = StatusCodes.Status503ServiceUnavailable,
        };
        problemDetails.Extensions["checks"] = checks;
        return context.RequestServices
            .GetRequiredService<IProblemDetailsService>()
            .WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problemDetails,
            })
            .AsTask();
    }
}

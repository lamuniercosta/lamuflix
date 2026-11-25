using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LamuFlix.Api.Endpoints;

internal static class VersionEndpoints
{
    private const string CommitVariable = "LAMUFLIX_COMMIT";
    private const string ReleaseVariable = "LAMUFLIX_RELEASE";
    private const string UnknownCommit = "unknown";

    internal static RouteGroupBuilder MapVersionEndpoints(this RouteGroupBuilder api)
    {
        ArgumentNullException.ThrowIfNull(api);
        var version = new VersionDto(
            NormalizeRelease(Environment.GetEnvironmentVariable(ReleaseVariable)),
            NormalizeCommit(Environment.GetEnvironmentVariable(CommitVariable)));

        api.MapGet("/version", () => TypedResults.Ok(version))
            .WithName("GetVersion")
            .WithSummary("Report the running release and commit metadata.")
            .Produces<VersionDto>();

        return api;
    }

    private static string? NormalizeRelease(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string NormalizeCommit(string? value) =>
        string.IsNullOrWhiteSpace(value) ? UnknownCommit : value;
}

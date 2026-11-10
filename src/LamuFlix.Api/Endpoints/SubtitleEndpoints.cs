using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LamuFlix.Api.Endpoints;

internal static class SubtitleEndpoints
{
    private const string CacheControlHeader = "Cache-Control";
    private const string NoStoreValue = "private, no-store";

    internal static RouteGroupBuilder MapSubtitleEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/movies/{id:int}/subtitles", ListSubtitleTracksAsync)
            .WithName("ListMovieSubtitleTracks")
            .WithSummary("List the eligible sidecar subtitle tracks of a movie.")
            .Produces<SubtitleTrackDto[]>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return api;
    }

    private static async Task<Ok<SubtitleTrackDto[]>> ListSubtitleTracksAsync(
        int id,
        HttpContext httpContext,
        IQueryHandler<ListSubtitleTracksQuery, ListSubtitleTracksResult> handler,
        CancellationToken cancellationToken)
    {
        EnforceNoStore(httpContext);

        if (!MovieId.TryCreate(id, out var movieId))
        {
            throw new BadHttpRequestException($"Movie id '{id}' is not a valid movie identifier.");
        }

        var result = await handler.HandleAsync(new ListSubtitleTracksQuery(movieId), cancellationToken);
        return TypedResults.Ok(result.Tracks
            .Select(track => new SubtitleTrackDto(track.Id, track.Label, track.Language, track.IsDefault))
            .ToArray());
    }

    /// <summary>
    /// Applies the frozen <c>private, no-store</c> policy on success and on every error response. The
    /// error is written later by the single <c>IExceptionHandler</c> and the ProblemDetails writer emits
    /// its own cache headers, so the value is re-applied while the response is starting to win over it.
    /// </summary>
    private static void EnforceNoStore(HttpContext httpContext)
    {
        httpContext.Response.Headers[CacheControlHeader] = NoStoreValue;
        httpContext.Response.OnStarting(
            static state =>
            {
                var response = (HttpResponse)state;
                response.Headers[CacheControlHeader] = NoStoreValue;
                return Task.CompletedTask;
            },
            httpContext.Response);
    }
}

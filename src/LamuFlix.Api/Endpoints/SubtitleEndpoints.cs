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
    private const string ContentTypeOptionsHeader = "X-Content-Type-Options";
    private const string NoSniffValue = "nosniff";
    private const string SubtitleContentType = "text/vtt; charset=utf-8";

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

        api.MapGet("/movies/{id:int}/subtitles/{trackId}/content", GetSubtitleContentAsync)
            .WithName("GetMovieSubtitleContent")
            .WithSummary("Serve one eligible sidecar subtitle track converted to WebVTT.")
            .Produces<string>(contentType: SubtitleContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
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

    private static async Task<ContentHttpResult> GetSubtitleContentAsync(
        int id,
        string trackId,
        HttpContext httpContext,
        IQueryHandler<GetSubtitleContentQuery, SubtitleContentResult> handler,
        CancellationToken cancellationToken)
    {
        EnforceNoStore(httpContext);
        EnforceNoSniff(httpContext);

        if (!MovieId.TryCreate(id, out var movieId))
        {
            throw new BadHttpRequestException($"Movie id '{id}' is not a valid movie identifier.");
        }

        var result = await handler.HandleAsync(new GetSubtitleContentQuery(movieId, trackId), cancellationToken);
        return TypedResults.Text(result.WebVtt, SubtitleContentType);
    }

    private static void EnforceNoSniff(HttpContext httpContext) =>
        ApplyHeader(httpContext, ContentTypeOptionsHeader, NoSniffValue);

    private static void EnforceNoStore(HttpContext httpContext) =>
        ApplyHeader(httpContext, CacheControlHeader, NoStoreValue);

    private static void ApplyHeader(HttpContext httpContext, string name, string value)
    {
        httpContext.Response.Headers[name] = value;
        httpContext.Response.OnStarting(
            static state =>
            {
                var (response, header, headerValue) = ((HttpResponse, string, string))state;
                response.Headers[header] = headerValue;
                return Task.CompletedTask;
            },
            (httpContext.Response, name, value));
    }
}

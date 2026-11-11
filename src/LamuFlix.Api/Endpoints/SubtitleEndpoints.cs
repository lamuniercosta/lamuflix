using System;
using System.Globalization;
using System.Linq;
using System.Text;
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
    private const string ContentDispositionHeader = "Content-Disposition";
    private const string SubtitleContentType = "text/vtt; charset=utf-8";
    private const string SidecarContentType = "application/x-subrip";
    private const string AttachmentPrefix = "attachment; filename=\"";
    private const string ExtendedFileNamePrefix = "\"; filename*=UTF-8''";
    private const char UnsafeAsciiReplacement = '_';

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

        api.MapPost("/movies/{id}/subtitles/prepare", PrepareSubtitleAsync)
            .WithName("PrepareMovieSubtitle")
            .WithSummary("Acquire the exact-basename sidecar subtitle of a movie when acquisition is enabled.")
            .Produces<SubtitlePrepareResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        api.MapGet("/movies/{id}/subtitles/sidecar", GetSubtitleSidecarAsync)
            .WithName("GetMovieSubtitleSidecar")
            .WithSummary("Serve the existing exact-basename sidecar subtitle of a movie without acquisition.")
            .Produces(StatusCodes.Status200OK, typeof(byte[]), SidecarContentType)
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

    private static async Task<Ok<SubtitlePrepareResponse>> PrepareSubtitleAsync(
        string id,
        HttpContext httpContext,
        ICommandHandler<PrepareSubtitleCommand, SubtitlePreparationResult> handler,
        CancellationToken cancellationToken)
    {
        EnforceNoStore(httpContext);

        var movieId = ParseMovieId(id);
        var result = await handler.HandleAsync(new PrepareSubtitleCommand(movieId), cancellationToken);
        return TypedResults.Ok(SubtitlePrepareResponse.From(result));
    }

    private static async Task<IResult> GetSubtitleSidecarAsync(
        string id,
        HttpContext httpContext,
        IQueryHandler<GetSubtitleSidecarQuery, SubtitleSidecarResult> handler,
        CancellationToken cancellationToken)
    {
        EnforceNoStore(httpContext);
        EnforceNoSniff(httpContext);

        var movieId = ParseMovieId(id);
        var result = await handler.HandleAsync(new GetSubtitleSidecarQuery(movieId), cancellationToken);
        ApplyHeader(httpContext, ContentDispositionHeader, Attachment(result.FileName));
        return TypedResults.Bytes(result.Content, SidecarContentType);
    }

    private static MovieId ParseMovieId(string rawId)
    {
        if (!int.TryParse(rawId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            || !MovieId.TryCreate(value, out var movieId))
        {
            throw new BadHttpRequestException($"Movie id '{rawId}' is not a valid movie identifier.");
        }

        return movieId;
    }

    private static string Attachment(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (IsSafeAsciiFileName(fileName))
        {
            return string.Concat(AttachmentPrefix, fileName, "\"");
        }

        var fallback = new StringBuilder(fileName.Length);
        foreach (var character in fileName)
        {
            fallback.Append(IsSafeAscii(character) ? character : UnsafeAsciiReplacement);
        }

        return string.Concat(
            AttachmentPrefix,
            fallback,
            ExtendedFileNamePrefix,
            Uri.EscapeDataString(fileName));
    }

    private static bool IsSafeAsciiFileName(string fileName)
    {
        foreach (var character in fileName)
        {
            if (!IsSafeAscii(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSafeAscii(char character) =>
        character is >= (char)0x20 and <= (char)0x7E && character is not ('"' or '\\');

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

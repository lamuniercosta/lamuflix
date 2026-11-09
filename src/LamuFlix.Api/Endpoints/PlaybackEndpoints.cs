using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Playback;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace LamuFlix.Api.Endpoints;

internal static class PlaybackEndpoints
{
    internal static RouteGroupBuilder MapPlaybackEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/movies/{id}/play", PlayMovieAsync)
            .WithName("PlayMovie")
            .WithSummary("Start local playback of a movie.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapGet("/playback/capabilities", GetPlaybackCapabilitiesAsync)
            .WithName("GetPlaybackCapabilities")
            .WithSummary("Report the effective local playback capability.")
            .Produces<PlaybackCapabilitiesDto>();

        return api;
    }

    private static async Task<Ok<PlaybackCapabilitiesDto>> GetPlaybackCapabilitiesAsync(
        IOptions<FeatureOptions> features,
        IQueryHandler<GetPlaybackCapabilitiesQuery, PlaybackCapabilitiesResult> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetPlaybackCapabilitiesQuery(features.Value.LocalPlay);
        var result = await handler.HandleAsync(query, cancellationToken);
        return TypedResults.Ok(new PlaybackCapabilitiesDto(result.LocalPlayEnabled));
    }

    private static async Task<NoContent> PlayMovieAsync(
        int id,
        HttpContext httpContext,
        ICommandHandler<PlayMovieCommand, Unit> handler,
        CancellationToken cancellationToken)
    {
        PlayRequestBoundary.EnsureProvenLocalRequest(httpContext);
        if (!MovieId.TryCreate(id, out var movieId))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Movie id must be greater than zero."],
            });
        }

        await handler.HandleAsync(new PlayMovieCommand(movieId), cancellationToken);
        return TypedResults.NoContent();
    }
}

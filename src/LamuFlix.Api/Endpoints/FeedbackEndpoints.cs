using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Feedback;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LamuFlix.Api.Endpoints;

internal static class FeedbackEndpoints
{
    internal static RouteGroupBuilder MapFeedbackEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/movies/{id}/watched", MarkWatchedAsync)
            .WithName("MarkWatched")
            .WithSummary("Mark a movie as watched.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapDelete("/movies/{id}/watched", UnmarkWatchedAsync)
            .WithName("UnmarkWatched")
            .WithSummary("Remove the watched mark from a movie.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapPost("/movies/{id}/not-interested", MarkNotInterestedAsync)
            .WithName("MarkNotInterested")
            .WithSummary("Mark a movie as not interested.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapDelete("/movies/{id}/not-interested", UnmarkNotInterestedAsync)
            .WithName("UnmarkNotInterested")
            .WithSummary("Remove the not-interested mark from a movie.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<NoContent> MarkWatchedAsync(
        int id,
        ICommandHandler<MarkWatchedCommand, Unit> handler,
        CancellationToken cancellationToken)
    {
        var movieId = ParseMovieId(id);
        await handler.HandleAsync(new MarkWatchedCommand(movieId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> UnmarkWatchedAsync(
        int id,
        ICommandHandler<UnmarkWatchedCommand, Unit> handler,
        CancellationToken cancellationToken)
    {
        var movieId = ParseMovieId(id);
        await handler.HandleAsync(new UnmarkWatchedCommand(movieId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> MarkNotInterestedAsync(
        int id,
        ICommandHandler<MarkNotInterestedCommand, Unit> handler,
        CancellationToken cancellationToken)
    {
        var movieId = ParseMovieId(id);
        await handler.HandleAsync(new MarkNotInterestedCommand(movieId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> UnmarkNotInterestedAsync(
        int id,
        ICommandHandler<UnmarkNotInterestedCommand, Unit> handler,
        CancellationToken cancellationToken)
    {
        var movieId = ParseMovieId(id);
        await handler.HandleAsync(new UnmarkNotInterestedCommand(movieId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static MovieId ParseMovieId(int id)
    {
        if (!MovieId.TryCreate(id, out var movieId))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Movie id must be greater than zero."],
            });
        }

        return movieId;
    }
}

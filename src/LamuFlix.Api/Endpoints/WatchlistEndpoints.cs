using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Watchlist;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LamuFlix.Api.Endpoints;

internal static class WatchlistEndpoints
{
    internal static RouteGroupBuilder MapWatchlistEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/movies/{id}/watchlist", AddToWatchlistAsync)
            .WithName("AddToWatchlist")
            .WithSummary("Add a movie to the watchlist.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapDelete("/movies/{id}/watchlist", RemoveFromWatchlistAsync)
            .WithName("RemoveFromWatchlist")
            .WithSummary("Remove a movie from the watchlist.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<NoContent> AddToWatchlistAsync(
        int id,
        ICommandHandler<AddToWatchlistCommand, Unit> handler,
        CancellationToken cancellationToken)
    {
        var movieId = ParseMovieId(id);
        await handler.HandleAsync(new AddToWatchlistCommand(movieId), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> RemoveFromWatchlistAsync(
        int id,
        ICommandHandler<RemoveFromWatchlistCommand, Unit> handler,
        CancellationToken cancellationToken)
    {
        var movieId = ParseMovieId(id);
        await handler.HandleAsync(new RemoveFromWatchlistCommand(movieId), cancellationToken);
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

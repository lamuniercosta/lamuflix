using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LamuFlix.Api.Endpoints;

internal static class RescanEndpoints
{
    internal static RouteGroupBuilder MapRescanEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/movies/{id}/rescan", RescanMovieAsync)
            .WithName("RescanMovie")
            .WithSummary("Re-probe a movie folder and refresh its stored format.")
            .Produces<RescanMovieResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<Ok<RescanMovieResponse>> RescanMovieAsync(
        int id,
        ICommandHandler<RescanMovieCommand, RescanMovieResult> handler,
        CancellationToken cancellationToken)
    {
        if (!MovieId.TryCreate(id, out var movieId))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Movie id must be greater than zero."],
            });
        }

        var result = await handler.HandleAsync(new RescanMovieCommand(movieId), cancellationToken);
        return TypedResults.Ok(RescanMovieResponse.From(result));
    }
}

internal sealed record RescanMovieResponse(int MovieId, bool Changed, string OldFormat, string NewFormat)
{
    public static RescanMovieResponse From(RescanMovieResult result) =>
        new(
            result.MovieId.Value,
            result.Changed,
            result.OldFormat.Extension,
            result.NewFormat.Extension);
}

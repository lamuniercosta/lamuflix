using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LamuFlix.Api.Endpoints;

internal static class EnrichmentEndpoints
{
    internal static RouteGroupBuilder MapEnrichmentEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/movies/{id}/enrichment", RequestEnrichmentAsync)
            .WithName("RequestEnrichment")
            .WithSummary("Request a metadata enrichment retry for a movie.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapPut("/movies/{id}/imdb", MatchImdbIdAsync)
            .WithName("MatchMovieImdbId")
            .WithSummary("Match a movie to a user-supplied IMDb id and request enrichment.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<Results<Accepted, ValidationProblem>> MatchImdbIdAsync(
        int id,
        MatchImdbIdRequest request,
        ICommandHandler<MatchMovieImdbIdCommand, MovieId> handler,
        CancellationToken cancellationToken)
    {
        if (!MovieId.TryCreate(id, out var movieId))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Movie id must be greater than zero."],
            });
        }

        if (!ImdbId.TryCreate(request.ImdbId, out var imdbId))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["imdbId"] = ["IMDb id must be 'tt' followed by 7 or 8 digits."],
            });
        }

        var matched = await handler.HandleAsync(new MatchMovieImdbIdCommand(movieId, imdbId), cancellationToken);
        return TypedResults.Accepted($"/api/movies/{matched.Value}");
    }

    private static async Task<Accepted> RequestEnrichmentAsync(
        int id,
        ICommandHandler<RequestEnrichmentCommand, MovieId> handler,
        CancellationToken cancellationToken)
    {
        if (!MovieId.TryCreate(id, out var movieId))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Movie id must be greater than zero."],
            });
        }

        var enriched = await handler.HandleAsync(new RequestEnrichmentCommand(movieId), cancellationToken);
        return TypedResults.Accepted($"/api/movies/{enriched.Value}");
    }
}

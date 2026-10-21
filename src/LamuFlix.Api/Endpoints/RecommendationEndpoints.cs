using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace LamuFlix.Api.Endpoints;

internal static class RecommendationEndpoints
{
    private const int DefaultTake = 10;
    private const string TakeKey = "take";

    internal static RouteGroupBuilder MapRecommendationEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/movies/{id:int}/recommendations", GetRecommendationsAsync)
            .WithName("GetRecommendations")
            .WithSummary("Get recommendations for a library movie")
            .Produces<List<RecommendedMovieDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Results<Ok<List<RecommendedMovieDto>>, BadRequest, NotFound>> GetRecommendationsAsync(
        int id,
        HttpRequest request,
        IMovieRepository movies,
        IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>> handler,
        IOptions<RecommendationOptions> options,
        CancellationToken cancellationToken)
    {
        var take = ParseTake(request.Query, options.Value.CandidatePoolSize);

        if (!MovieId.TryCreate(id, out var movieId))
        {
            throw new NotFoundException();
        }

        cancellationToken.ThrowIfCancellationRequested();

        var selected = await movies.GetAsync(movieId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (selected is null)
        {
            throw new NotFoundException();
        }

        if (selected.Status == EnrichmentStatus.Pending)
        {
            return TypedResults.Ok(new List<RecommendedMovieDto>());
        }

        var recommended = await handler.HandleAsync(new GetRecommendationsQuery(movieId, take), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return TypedResults.Ok<List<RecommendedMovieDto>>([.. recommended.Select(ToDto)]);
    }

    private static int ParseTake(IQueryCollection query, int candidatePoolSize)
    {
        if (!query.TryGetValue(TakeKey, out var values))
        {
            return ValidateTake(DefaultTake, candidatePoolSize);
        }

        if (values.Count != 1
            || !int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new BadHttpRequestException($"'{TakeKey}' must be a single integer.");
        }

        return ValidateTake(parsed, candidatePoolSize);
    }

    private static int ValidateTake(int take, int candidatePoolSize)
    {
        if (take < 1 || take > candidatePoolSize)
        {
            throw new BadHttpRequestException(
                $"'{TakeKey}' must be between 1 and the configured candidate pool size.");
        }

        return take;
    }

    private static RecommendedMovieDto ToDto(RecommendedMovie recommendedMovie)
    {
        var summary = recommendedMovie.Summary;

        return new RecommendedMovieDto(
            summary.Id.Value,
            summary.Title,
            summary.PosterUrl,
            summary.ReleaseYear,
            summary.Runtime,
            summary.ImdbRating,
            summary.EnrichmentStatus,
            summary.IsInWatchlist,
            summary.IsWatched,
            summary.IsNotInterested,
            recommendedMovie.Score,
            [.. recommendedMovie.Reasons.Select(reason => reason.Text)]);
    }
}

using System;
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
    private const string OffsetKey = "offset";
    private const string ExcludeKey = "exclude";

    internal static RouteGroupBuilder MapRecommendationEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/movies/{id:int}/recommendations", GetRecommendationsAsync)
            .WithName("GetRecommendations")
            .WithSummary("Get recommendations for a library movie")
            .Produces<List<RecommendedMovieDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        api.MapGet("/recommendations/for-you", GetForYouRecommendationsAsync)
            .WithName("GetForYouRecommendations")
            .WithSummary("Get personalized recommendations from the taste profile")
            .Produces<ForYouRecommendationsPageDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<Results<Ok<ForYouRecommendationsPageDto>, BadRequest>> GetForYouRecommendationsAsync(
        HttpRequest request,
        IQueryHandler<GetForYouRecommendationsQuery, ForYouRecommendationsPage> handler,
        IOptions<RecommendationOptions> options,
        CancellationToken cancellationToken)
    {
        var take = ParseTake(request.Query, options.Value.CandidatePoolSize);
        var offset = ParseOffset(request.Query);
        var excludedMovieIds = ParseExcludedMovieIds(request.Query, options.Value.MaxExcludeCount);

        cancellationToken.ThrowIfCancellationRequested();

        var page = await handler.HandleAsync(
            new GetForYouRecommendationsQuery(take, offset, excludedMovieIds), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return TypedResults.Ok(new ForYouRecommendationsPageDto(
            [.. page.Items.Select(item => ToDto(item, item.StartedCollectionId))],
            page.HasMore,
            page.HasSeeds));
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

        return TypedResults.Ok<List<RecommendedMovieDto>>([.. recommended.Select(item => ToDto(item))]);
    }

    private static int ParseTake(IQueryCollection query, int candidatePoolSize)
    {
        var take = DefaultTake;

        if (query.TryGetValue(TakeKey, out var values))
        {
            if (values.Count != 1
                || !int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new BadHttpRequestException($"'{TakeKey}' must be a single integer.");
            }

            take = parsed;
        }

        if (take < 1 || take > candidatePoolSize)
        {
            throw new BadHttpRequestException(
                $"'{TakeKey}' must be between 1 and the configured candidate pool size.");
        }

        return take;
    }

    private static int ParseOffset(IQueryCollection query)
    {
        if (!query.TryGetValue(OffsetKey, out var values))
        {
            return 0;
        }

        if (values.Count != 1
            || !int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var offset)
            || offset < 0)
        {
            throw new BadHttpRequestException($"'{OffsetKey}' must be a single nonnegative integer.");
        }

        return offset;
    }

    private static ImmutableHashSet<MovieId> ParseExcludedMovieIds(IQueryCollection query, int maxExcludeCount)
    {
        if (!query.TryGetValue(ExcludeKey, out var values))
        {
            return ImmutableHashSet<MovieId>.Empty;
        }

        if (values.Count > maxExcludeCount)
        {
            throw new BadHttpRequestException($"'{ExcludeKey}' must contain no more than the configured maximum number of values.");
        }

        var ids = ImmutableHashSet.CreateBuilder<MovieId>();
        foreach (var value in values)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                || !MovieId.TryCreate(id, out var movieId))
            {
                throw new BadHttpRequestException($"'{ExcludeKey}' values must be valid movie IDs.");
            }

            ids.Add(movieId);
        }

        return ids.ToImmutable();
    }

    private static RecommendedMovieDto ToDto(RecommendedMovie recommendedMovie, int? startedCollectionId = null)
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
            summary.Review.Name,
            recommendedMovie.Score,
            [.. recommendedMovie.Evidence.Select(ToEvidenceDto)],
            ToSeedDto(recommendedMovie.Seed),
            startedCollectionId);
    }

    private static RecommendationSeedDto? ToSeedDto(RecommendationSeed? seed) =>
        seed is null ? null : new RecommendationSeedDto(seed.MovieId, seed.Title, Signal(seed.Signal));

    private static RecommendationSeedSignal Signal(SeedSignal signal) =>
        signal switch
        {
            SeedSignal.Liked => RecommendationSeedSignal.Liked,
            SeedSignal.WatchedWithoutReview => RecommendationSeedSignal.Watched,
            SeedSignal.Watchlist => RecommendationSeedSignal.Watchlist,
            _ => throw new ArgumentOutOfRangeException(nameof(signal), signal, "Unknown seed signal."),
        };

    private static RecommendationEvidenceDto ToEvidenceDto(RecommendationEvidence evidence) =>
        new(evidence.Criterion, evidence.Text, evidence.MatchedNames);
}

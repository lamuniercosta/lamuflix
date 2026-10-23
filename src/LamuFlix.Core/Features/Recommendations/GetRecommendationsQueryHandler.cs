using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class GetRecommendationsQueryHandler(
    IMovieRepository movies,
    IRecommendationCandidateSource candidates,
    GenreWeightsCalculator genreWeightsCalculator,
    IOptions<RecommendationOptions> options)
    : IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>
{
    private const string SimilarStoryReason = "Similar story";

    public async Task<ImmutableList<RecommendedMovie>> HandleAsync(
        GetRecommendationsQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selected = await movies.GetAsync(query.MovieId, cancellationToken)
            ?? throw new NotFoundException();
        cancellationToken.ThrowIfCancellationRequested();

        var pool = await candidates.GetCandidatesAsync(query.MovieId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (pool.Count == 0)
        {
            return ImmutableList<RecommendedMovie>.Empty;
        }

        var genreWeights = await genreWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var weights = options.Value.Weights;
        var storyThreshold = options.Value.SimilarStoryThreshold;
        var selectedFeatures = MovieFeaturesMapper.From(selected);
        var scored = Score(selectedFeatures, pool, weights, genreWeights, storyThreshold, cancellationToken);

        return
        [
            .. scored
                .OrderByDescending(item => item.Result.Score)
                .ThenBy(item => item.Movie.Id.Value)
                .Take(query.Take)
                .Select(item => new RecommendedMovie(
                    Summarize(item.Movie),
                    item.Result.Score,
                    item.Result.Reasons)),
        ];
    }

    private static List<ScoredCandidate> Score(
        MovieFeatures selectedFeatures,
        IReadOnlyList<RecommendationCandidate> pool,
        SimilarityWeights weights,
        GenreWeights genreWeights,
        double storyThreshold,
        CancellationToken cancellationToken)
    {
        var scored = new List<ScoredCandidate>(pool.Count);

        foreach (var candidate in pool)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = SimilarityScorer.Score(
                selectedFeatures,
                MovieFeaturesMapper.From(candidate.Movie),
                weights,
                genreWeights);

            if (candidate.CosineSimilarity > storyThreshold)
            {
                result = result with { Reasons = result.Reasons.Add(Reason.Structural(SimilarStoryReason)) };
            }

            scored.Add(new ScoredCandidate(candidate.Movie, result));
        }

        return scored;
    }

    private static MovieSummary Summarize(Movie movie)
    {
        var metadata = movie.Metadata;

        return new MovieSummary(
            movie.Id,
            movie.Title,
            PosterUrl(metadata),
            ReleaseYear(metadata),
            RuntimeMinutes(metadata),
            ImdbRating(metadata),
            movie.Status,
            movie.IsInWatchlist,
            movie.IsWatched,
            movie.IsNotInterested);
    }

    private static string? PosterUrl(MovieMetadata? metadata) => metadata?.PosterUrl;

    private static int? ReleaseYear(MovieMetadata? metadata) => metadata?.ReleaseYear?.Value;

    private static int? RuntimeMinutes(MovieMetadata? metadata) => metadata?.Runtime?.Minutes;

    private static decimal? ImdbRating(MovieMetadata? metadata) => metadata?.ImdbRating?.Value;

    private readonly record struct ScoredCandidate(Movie Movie, SimilarityResult Result);
}

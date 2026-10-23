using System;
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
    IRecommendationProfileSource profileSource,
    GenreWeightsCalculator genreWeightsCalculator,
    TimeProvider timeProvider,
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

        var eligible = ExcludeNotInterested(pool, cancellationToken);

        if (eligible.Count == 0)
        {
            return ImmutableList<RecommendedMovie>.Empty;
        }

        var genreWeights = await genreWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var profileInputs = await profileSource.GetProfileInputsAsync(
            [.. eligible.Select(candidate => candidate.Movie.Id)],
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var recommendationOptions = options.Value;
        var profile = new TasteProfileCalculator(timeProvider, recommendationOptions).Calculate(
            profileInputs.Movies,
            genreWeights);
        var selectedFeatures = MovieFeaturesMapper.From(selected);
        var scored = Score(
            selectedFeatures,
            eligible,
            profile,
            profileInputs.CandidateEmbeddings,
            genreWeights,
            recommendationOptions,
            cancellationToken);

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

    private static List<RecommendationCandidate> ExcludeNotInterested(
        IReadOnlyList<RecommendationCandidate> pool,
        CancellationToken cancellationToken)
    {
        var eligible = new List<RecommendationCandidate>(pool.Count);

        foreach (var candidate in pool)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!candidate.Movie.IsNotInterested)
            {
                eligible.Add(candidate);
            }
        }

        return eligible;
    }

    private static List<ScoredCandidate> Score(
        MovieFeatures selectedFeatures,
        IReadOnlyList<RecommendationCandidate> pool,
        TasteProfile profile,
        ImmutableDictionary<MovieId, ImmutableArray<float>> candidateEmbeddings,
        GenreWeights genreWeights,
        RecommendationOptions options,
        CancellationToken cancellationToken)
    {
        var scored = new List<ScoredCandidate>(pool.Count);

        foreach (var candidate in pool)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var features = MovieFeaturesMapper.From(candidate.Movie);
            var similarity = SimilarityScorer.Score(
                selectedFeatures,
                features,
                options.Weights,
                genreWeights);

            var profileSimilarity = ProfileSimilarityCalculator.Similarity(
                [.. features.Genres.Order(StringComparer.Ordinal)],
                candidateEmbeddings.GetValueOrDefault(
                    candidate.Movie.Id,
                    ImmutableArray<float>.Empty),
                profile,
                genreWeights);

            var score = BlendScorer.Score(
                similarity.Score,
                profileSimilarity,
                candidate.Movie.IsWatched ? 1.0 : 0.0,
                0.0,
                options);

            if (candidate.CosineSimilarity > options.SimilarStoryThreshold)
            {
                similarity = similarity with
                {
                    Reasons = similarity.Reasons.Add(Reason.Structural(SimilarStoryReason)),
                };
            }

            scored.Add(new ScoredCandidate(
                candidate.Movie,
                new SimilarityResult(score, similarity.Reasons)));
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

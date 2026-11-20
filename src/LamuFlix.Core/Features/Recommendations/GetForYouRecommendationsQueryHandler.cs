using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class GetForYouRecommendationsQueryHandler(
    IMovieCatalog catalog,
    IMovieRepository movies,
    IRecommendationProfileSource profileSource,
    GenreWeightsCalculator genreWeightsCalculator,
    KeywordWeightsCalculator keywordWeightsCalculator,
    IOptions<RecommendationOptions> options)
    : IQueryHandler<GetForYouRecommendationsQuery, ForYouRecommendationsPage>
{
    private const int BrowsePageSize = 100;

    public async Task<ForYouRecommendationsPage> HandleAsync(
        GetForYouRecommendationsQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var eligible = await AcquireEligibleMoviesAsync(cancellationToken);
        if (query.ExcludedMovieIds is { Count: > 0 } excludedMovieIds)
        {
            eligible.RemoveAll(movie => excludedMovieIds.Contains(movie.Id));
        }
        cancellationToken.ThrowIfCancellationRequested();

        if (eligible.Count == 0)
        {
            return new ForYouRecommendationsPage(ImmutableList<RecommendedMovie>.Empty, false);
        }

        var profile = await profileSource.GetTasteProfileAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var profileInputs = await profileSource.GetProfileInputsAsync(
            [.. eligible.Select(movie => movie.Id)],
            [.. profile.LikedSources.Select(source => source.MovieId)],
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (!HasPositiveSignal(profileInputs.Movies))
        {
            return new ForYouRecommendationsPage(ImmutableList<RecommendedMovie>.Empty, false);
        }

        var genreWeights = await genreWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var keywordCounts = await keywordWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var recommendationOptions = options.Value;
        var scored = Score(
            eligible,
            profile,
            profileInputs,
            genreWeights,
            recommendationOptions,
            cancellationToken);

        scored = ApplyCollectionCut(scored, recommendationOptions.MaxPerCollection);

        if (query.Offset >= scored.Count)
        {
            return new ForYouRecommendationsPage(ImmutableList<RecommendedMovie>.Empty, false);
        }

        var relevanceOffset = BlendScorer.NonNegativeOffset(scored.Select(item => item.Result.Score));
        var take = Math.Min(query.Take, recommendationOptions.CandidatePoolSize);
        var selectionLimit = (int)Math.Min(
            scored.Count,
            (long)query.Offset + take + 1);
        var selectedIds = MmrSelector.Select(
            [.. scored.Select(item => new MmrCandidate(
                item.Movie.Id,
                item.Result.Score + relevanceOffset,
                item.Features,
                item.BestSeedId))],
            selectionLimit,
            recommendationOptions.MmrLambda,
            recommendationOptions,
            genreWeights,
            keywordCounts,
            recommendationOptions.MaxPerBestSeed,
            recommendationOptions.MaxPerCollection);

        var byId = scored.ToDictionary(item => item.Movie.Id);

        var items = selectedIds
                .Select(id => byId[id])
                .Skip(query.Offset)
                .Take(take)
                .Select(item => new RecommendedMovie(
                    MovieSummaryMapper.From(item.Movie),
                    item.Result.Score,
                    item.Result.Reasons))
                .ToImmutableList();

        return new ForYouRecommendationsPage(items, selectedIds.Count > (long)query.Offset + take);
    }

    private async Task<List<Movie>> AcquireEligibleMoviesAsync(CancellationToken cancellationToken)
    {
        var eligible = new List<Movie>();
        var seen = new HashSet<MovieId>();

        for (var pageNumber = 1; ; pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await catalog.BrowseAsync(
                new MovieQuery
                {
                    Sort = MovieSort.Title,
                    Direction = SortDirection.Ascending,
                    Page = new Page(pageNumber, BrowsePageSize),
                },
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var summary in page.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsEligible(summary.IsWatched, summary.Review))
                {
                    await AddEligibleAsync(eligible, seen, summary.Id, cancellationToken);
                }
            }

            if (page.Items.Length < BrowsePageSize)
            {
                return eligible;
            }
        }
    }

    private async Task AddEligibleAsync(
        List<Movie> eligible,
        HashSet<MovieId> seen,
        MovieId id,
        CancellationToken cancellationToken)
    {
        if (!seen.Add(id))
        {
            return;
        }

        var movie = await movies.GetAsync(id, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (movie is not null && IsEligible(movie.IsWatched, movie.Review))
        {
            eligible.Add(movie);
        }
    }

    private static bool IsEligible(bool isWatched, Review review) =>
        !isWatched && !review.IsDisliked;

    private static bool HasPositiveSignal(ImmutableArray<TasteProfileMovie> movies)
    {
        if (movies.IsDefaultOrEmpty)
        {
            return false;
        }

        foreach (var movie in movies)
        {
            if (movie.IsWatched || !movie.PlayedAt.IsDefaultOrEmpty || movie.Review.IsLiked)
            {
                return true;
            }
        }

        return false;
    }

    private static List<ScoredCandidate> Score(
        IReadOnlyList<Movie> eligible,
        TasteProfile profile,
        RecommendationProfileInputs profileInputs,
        GenreWeights genreWeights,
        RecommendationOptions options,
        CancellationToken cancellationToken)
    {
        var scored = new List<ScoredCandidate>(eligible.Count);
        var candidateEmbeddings = profileInputs.CandidateEmbeddings;

        foreach (var movie in eligible)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scoringFeatures = MovieFeaturesMapper.From(movie);
            var genres = RecommendationScoring.Genres(scoringFeatures);
            var embedding = candidateEmbeddings.GetValueOrDefault(movie.Id, ImmutableArray<float>.Empty);
            var features = MovieFeaturesMapper.From(movie, includeSignals: true, plotEmbedding: embedding);

            var profileSimilarity = ProfileSimilarityCalculator.Evaluate(
                genres,
                embedding,
                profile,
                genreWeights);

            var reviewAffinity = ProfileSimilarityCalculator.EvaluateReviewAffinity(
                genres,
                RecommendationScoring.Cast(scoringFeatures),
                RecommendationScoring.Directors(scoringFeatures),
                embedding,
                profile,
                profileInputs.LikedSourceEmbeddings,
                options.Weights,
                genreWeights);

            var contribution = RecommendationScoring.ProfileContribution(
                profileSimilarity.Similarity,
                reviewAffinity,
                options);

            var score = BlendScorer.ScoreWithProfileContribution(
                0.0,
                contribution,
                movie.IsWatched ? 1.0 : 0.0,
                0.0,
                options);

            scored.Add(new ScoredCandidate(
                movie,
                features,
                reviewAffinity.LikedSource?.MovieId,
                new SimilarityResult(
                    score,
                    BuildReasons(profileSimilarity, reviewAffinity, contribution, options))));
        }

        return scored;
    }

    private static List<ScoredCandidate> ApplyCollectionCut(
        IReadOnlyList<ScoredCandidate> candidates,
        int maxPerCollection)
    {
        var retained = new HashSet<MovieId>();
        foreach (var group in candidates
                     .GroupBy(candidate => candidate.Features.CollectionId)
                     .Where(group => group.Key.HasValue))
        {
            foreach (var candidate in group
                         .OrderBy(candidate => candidate.Features.ReleaseYear is null)
                         .ThenBy(candidate => candidate.Features.ReleaseYear?.Value)
                         .ThenBy(candidate => candidate.Movie.Id.Value)
                         .Take(maxPerCollection))
            {
                retained.Add(candidate.Movie.Id);
            }
        }

        return [.. candidates.Where(candidate =>
            candidate.Features.CollectionId is null || retained.Contains(candidate.Movie.Id))];
    }


    private static ImmutableList<Reason> BuildReasons(
        ProfileSimilarityEvidence profileSimilarity,
        ReviewAffinityEvidence reviewAffinity,
        double profileContribution,
        RecommendationOptions options)
    {
        var builder = ImmutableList.CreateBuilder<Reason>();

        if (options.BlendProfileWeight > 0.0)
        {
            RecommendationReasonBuilder.AddProfileReasons(builder, profileSimilarity);
            RecommendationReasonBuilder.AddLikedReviewReason(
                builder,
                reviewAffinity,
                profileContribution,
                options);
        }

        RecommendationReasonBuilder.EnsureFallbackReason(builder);

        return builder.ToImmutable();
    }





    private readonly record struct ScoredCandidate(
        Movie Movie,
        MovieFeatures Features,
        MovieId? BestSeedId,
        SimilarityResult Result);
}

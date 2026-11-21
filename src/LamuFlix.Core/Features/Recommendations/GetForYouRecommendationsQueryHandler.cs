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

public sealed class GetForYouRecommendationsQueryHandler(
    IRecommendationCandidateSource candidates,
    IRecommendationProfileSource profileSource,
    GenreWeightsCalculator genreWeightsCalculator,
    KeywordWeightsCalculator keywordWeightsCalculator,
    IOptions<RecommendationOptions> options)
    : IQueryHandler<GetForYouRecommendationsQuery, ForYouRecommendationsPage>
{
    public async Task<ForYouRecommendationsPage> HandleAsync(
        GetForYouRecommendationsQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var library = await candidates.GetLibraryFeaturesAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var eligible = Eligible(library, query.ExcludedMovieIds);
        if (eligible.Count == 0)
        {
            return EmptyPage;
        }

        var profile = await profileSource.GetTasteProfileAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (!HasPositiveSignal(library, profile))
        {
            return EmptyPage;
        }

        var genreWeights = await genreWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var keywordCounts = await keywordWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var recommendationOptions = options.Value;
        var scored = Score(
            eligible,
            profile,
            LikedSourceEmbeddings(library, profile),
            genreWeights,
            recommendationOptions,
            cancellationToken);

        scored = ApplyCollectionCut(scored, recommendationOptions.MaxPerCollection);

        if (query.Offset >= scored.Count)
        {
            return EmptyPage;
        }

        var relevanceOffset = BlendScorer.NonNegativeOffset(scored.Select(item => item.Result.Score));
        var take = Math.Min(query.Take, recommendationOptions.CandidatePoolSize);
        var selectionLimit = (int)Math.Min(
            scored.Count,
            (long)query.Offset + take + 1);
        var selectedIds = MmrSelector.Select(
            [.. scored.Select(item => new MmrCandidate(
                item.Entry.Id,
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

        var byId = scored.ToDictionary(item => item.Entry.Id);

        var items = selectedIds
                .Select(id => byId[id])
                .Skip(query.Offset)
                .Take(take)
                .Select(item => new RecommendedMovie(
                    item.Entry.Summary,
                    item.Result.Score,
                    item.Result.Reasons))
                .ToImmutableList();

        return new ForYouRecommendationsPage(items, selectedIds.Count > (long)query.Offset + take);
    }

    private static ForYouRecommendationsPage EmptyPage =>
        new(ImmutableList<RecommendedMovie>.Empty, false);

    private static List<RecommendationLibraryEntry> Eligible(
        IReadOnlyList<RecommendationLibraryEntry> library,
        IReadOnlyCollection<MovieId>? excludedMovieIds)
    {
        var eligible = new List<RecommendationLibraryEntry>(library.Count);

        foreach (var entry in library)
        {
            if (IsEligible(entry.Review, entry.IsWatched)
                && (excludedMovieIds is null || !excludedMovieIds.Contains(entry.Id)))
            {
                eligible.Add(entry);
            }
        }

        return eligible;
    }

    private static bool IsEligible(Review review, bool isWatched) =>
        !isWatched && !review.IsDisliked;

    private static bool HasPositiveSignal(
        IReadOnlyList<RecommendationLibraryEntry> library,
        TasteProfile profile)
    {
        if (!profile.PlaybackByMovieId.IsEmpty || !profile.LikedSources.IsDefaultOrEmpty)
        {
            return true;
        }

        foreach (var entry in library)
        {
            if (entry.IsWatched || entry.Review.IsLiked)
            {
                return true;
            }
        }

        return false;
    }

    private static List<ScoredCandidate> Score(
        IReadOnlyList<RecommendationLibraryEntry> eligible,
        TasteProfile profile,
        ImmutableDictionary<MovieId, ImmutableArray<float>> likedSourceEmbeddings,
        GenreWeights genreWeights,
        RecommendationOptions options,
        CancellationToken cancellationToken)
    {
        var scored = new List<ScoredCandidate>(eligible.Count);

        foreach (var entry in eligible)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var features = entry.Features;
            var genres = RecommendationScoring.Genres(features);

            var profileSimilarity = ProfileSimilarityCalculator.Evaluate(
                genres,
                features.PlotEmbedding,
                profile,
                genreWeights);

            var reviewAffinity = ProfileSimilarityCalculator.EvaluateReviewAffinity(
                genres,
                RecommendationScoring.Cast(features),
                RecommendationScoring.Directors(features),
                features.PlotEmbedding,
                profile,
                likedSourceEmbeddings,
                options.Weights,
                genreWeights);

            var contribution = RecommendationScoring.ProfileContribution(
                profileSimilarity.Similarity,
                reviewAffinity,
                options);

            var score = BlendScorer.ScoreWithProfileContribution(
                0.0,
                contribution,
                0.0,
                0.0,
                options);

            scored.Add(new ScoredCandidate(
                entry,
                features,
                reviewAffinity.LikedSource?.MovieId,
                new SimilarityResult(
                    score,
                    BuildReasons(profileSimilarity, reviewAffinity, contribution, options))));
        }

        return scored;
    }

    private static ImmutableDictionary<MovieId, ImmutableArray<float>> LikedSourceEmbeddings(
        IReadOnlyList<RecommendationLibraryEntry> library,
        TasteProfile profile)
    {
        if (profile.LikedSources.IsDefaultOrEmpty)
        {
            return ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty;
        }

        var likedSourceIds = new HashSet<MovieId>(
            profile.LikedSources.Select(source => source.MovieId));

        var builder = ImmutableDictionary.CreateBuilder<MovieId, ImmutableArray<float>>();

        foreach (var entry in library)
        {
            if (likedSourceIds.Contains(entry.Id) && !entry.Features.PlotEmbedding.IsDefaultOrEmpty)
            {
                builder[entry.Id] = entry.Features.PlotEmbedding;
            }
        }

        return builder.ToImmutable();
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
                         .ThenBy(candidate => candidate.Entry.Id.Value)
                         .Take(maxPerCollection))
            {
                retained.Add(candidate.Entry.Id);
            }
        }

        return [.. candidates.Where(candidate =>
            candidate.Features.CollectionId is null || retained.Contains(candidate.Entry.Id))];
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
        RecommendationLibraryEntry Entry,
        MovieFeatures Features,
        MovieId? BestSeedId,
        SimilarityResult Result);
}

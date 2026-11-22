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
    TimeProvider timeProvider,
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

        var recommendationOptions = options.Value;

        var genreWeights = await genreWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var keywordCounts = await keywordWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var keywordWeights = SimilarityScorer.PrepareKeywordWeights(recommendationOptions, keywordCounts);
        var decay = new RecommendationRecencyDecay(timeProvider, recommendationOptions);
        var ranked = NearestSeedScorer.Rank(
            eligible,
            library,
            profile,
            recommendationOptions,
            decay,
            genreWeights,
            keywordWeights,
            cancellationToken);

        var retained = ApplyCollectionCut(ranked, recommendationOptions.MaxPerCollection);
        if (query.Offset >= retained.Count)
        {
            return EmptyPage;
        }

        var take = Math.Min(query.Take, recommendationOptions.CandidatePoolSize);
        var items = retained
            .Skip(query.Offset)
            .Take(take)
            .Select(match => ToRecommendedMovie(match, recommendationOptions))
            .ToImmutableList();

        return new ForYouRecommendationsPage(items, retained.Count > (long)query.Offset + take);
    }

    private static ForYouRecommendationsPage EmptyPage =>
        new(ImmutableList<RecommendedMovie>.Empty, false);

    private static RecommendedMovie ToRecommendedMovie(NearestSeedMatch match, RecommendationOptions recommendationOptions) =>
        new(
            match.Candidate.Summary,
            match.Score,
            SimilarityScorer.BuildEvidence(match.Pair, recommendationOptions),
            new RecommendationSeed(
                match.SeedMovieId.Value,
                SimilarityScorer.SanitizeDisplayText(match.SeedTitle),
                match.Signal));

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

    private static ImmutableList<NearestSeedMatch> ApplyCollectionCut(
        IReadOnlyList<NearestSeedMatch> candidates,
        int maxPerCollection)
    {
        var retained = new HashSet<MovieId>();

        foreach (var group in candidates
                     .GroupBy(candidate => candidate.Candidate.Features.CollectionId)
                     .Where(group => group.Key.HasValue))
        {
            foreach (var candidate in group
                         .OrderBy(candidate => candidate.Candidate.Features.ReleaseYear is null)
                         .ThenBy(candidate => candidate.Candidate.Features.ReleaseYear?.Value)
                         .ThenBy(candidate => candidate.Candidate.Id.Value)
                         .Take(maxPerCollection))
            {
                retained.Add(candidate.Candidate.Id);
            }
        }

        return
        [
            .. candidates.Where(candidate =>
                candidate.Candidate.Features.CollectionId is null || retained.Contains(candidate.Candidate.Id)),
        ];
    }
}

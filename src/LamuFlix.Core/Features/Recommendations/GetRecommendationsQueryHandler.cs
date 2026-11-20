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
    KeywordWeightsCalculator keywordWeightsCalculator,
    IOptions<RecommendationOptions> options)
    : IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>
{
    public async Task<ImmutableList<RecommendedMovie>> HandleAsync(
        GetRecommendationsQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selected = await movies.GetAsync(query.MovieId, cancellationToken)
            ?? throw new NotFoundException();
        cancellationToken.ThrowIfCancellationRequested();

        var library = await candidates.GetLibraryFeaturesAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var recommendationOptions = options.Value;
        var partitioned = Partition(library, query.MovieId, cancellationToken);
        if (partitioned.SelectedVector is { } unusableSelectedVector && !IsUsable(unusableSelectedVector))
        {
            return ImmutableList<RecommendedMovie>.Empty;
        }

        var eligible = partitioned.Eligible;
        if (eligible.Count == 0)
        {
            return ImmutableList<RecommendedMovie>.Empty;
        }

        var genreWeights = await genreWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var keywordCounts = await keywordWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var profile = await profileSource.GetTasteProfileAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        // Candidate vectors come from the library load, so the profile-input fetch only needs the selected
        // fallback (when the selected movie is absent from the enriched load) and the liked sources.
        var profileInputs = await profileSource.GetProfileInputsAsync(
            partitioned.SelectedInLoad ? [] : [query.MovieId],
            [.. profile.LikedSources.Select(source => source.MovieId)],
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var selectedVector = partitioned.SelectedVector;
        if (selectedVector is null)
        {
            selectedVector =
            [
                .. profileInputs.CandidateEmbeddings.GetValueOrDefault(
                    query.MovieId,
                    ImmutableArray<float>.Empty),
            ];
            if (!IsUsable(selectedVector))
            {
                return ImmutableList<RecommendedMovie>.Empty;
            }
        }

        var selectedFeatures = MovieFeaturesMapper.From(
            selected,
            includeSignals: true,
            plotEmbedding: selectedVector);

        // Map the validated option tokens to the DEV-518 breakdown names once per request.
        var strongReasonCriteria = new HashSet<string>(
            recommendationOptions.StrongReasonCriteria ?? [],
            System.StringComparer.OrdinalIgnoreCase);

        var scored = Score(
            selectedFeatures,
            library,
            query.MovieId,
            profile,
            profileInputs,
            strongReasonCriteria,
            genreWeights,
            keywordCounts,
            recommendationOptions,
            cancellationToken);

        var relevanceOffset = BlendScorer.NonNegativeOffset(scored.Select(item => item.Result.Score));
        var aboveBar = scored.Where(item => item.Tier == MatchTier.AboveBar).ToList();
        var strongReason = scored.Where(item => item.Tier == MatchTier.StrongReason).ToList();

        var selectedIds = SelectTier(
            aboveBar,
            query.Take,
            selectedFeatures,
            recommendationOptions,
            genreWeights,
            keywordCounts,
            relevanceOffset);
        var strongReasonBudget = System.Math.Max(0, query.Take - selectedIds.Count);
        var strongReasonIds = SelectTier(
            strongReason,
            strongReasonBudget,
            selectedFeatures,
            recommendationOptions,
            genreWeights,
            keywordCounts,
            relevanceOffset);

        var byId = scored.ToDictionary(item => item.Library.Id);

        return
        [
            .. selectedIds
                .Concat(strongReasonIds)
                .Select(id => byId[id])
                .Select(item => new RecommendedMovie(
                    item.Library.Summary,
                    item.Result.Score,
                    item.Result.Reasons)),
        ];
    }

    private static PartitionedLibrary Partition(
        IReadOnlyList<RecommendationLibraryEntry> library,
        MovieId selectedMovieId,
        CancellationToken cancellationToken)
    {
        var eligible = new List<RecommendationLibraryEntry>(library.Count);
        var selectedInLoad = false;
        float[]? selectedVector = null;

        foreach (var entry in library)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Id == selectedMovieId)
            {
                selectedInLoad = true;
                selectedVector = [.. entry.Features.PlotEmbedding];
                continue;
            }

            if (!entry.Review.IsDisliked)
            {
                eligible.Add(entry);
            }
        }

        return new PartitionedLibrary(eligible, selectedInLoad, selectedVector);
    }

    private static bool IsUsable(float[]? vector)
    {
        if (vector is null || vector.Length != EmbeddingDimensions.Value)
        {
            return false;
        }

        var normSquared = 0.0;
        foreach (var value in vector)
        {
            if (!float.IsFinite(value))
            {
                return false;
            }

            normSquared += value * value;
        }

        return normSquared > 0.0;
    }

    private static ImmutableList<MovieId> SelectTier(
        IReadOnlyList<ScoredCandidate> tier,
        int budget,
        MovieFeatures selectedFeatures,
        RecommendationOptions options,
        GenreWeights genreWeights,
        KeywordCounts keywordCounts,
        double relevanceOffset)
    {
        if (budget <= 0 || tier.Count == 0)
        {
            return ImmutableList<MovieId>.Empty;
        }

        var mmrCandidates = tier
            .Select(item => new MmrCandidate(
                item.Library.Id,
                item.Result.Score + relevanceOffset,
                item.Features))
            .ToList();
        var mmrOrder = MmrSelector.Select(
            mmrCandidates,
            budget,
            options.MmrLambda,
            options,
            genreWeights,
            keywordCounts);

        return SeedCollectionGrouper.Group(
            mmrCandidates,
            mmrOrder,
            selectedFeatures,
            budget,
            options,
            genreWeights,
            keywordCounts);
    }

    private static List<ScoredCandidate> Score(
        MovieFeatures selectedFeatures,
        IReadOnlyList<RecommendationLibraryEntry> library,
        MovieId selectedMovieId,
        TasteProfile profile,
        RecommendationProfileInputs profileInputs,
        IReadOnlySet<string> strongReasonCriteria,
        GenreWeights genreWeights,
        KeywordCounts keywordCounts,
        RecommendationOptions options,
        CancellationToken cancellationToken)
    {
        var scored = new List<ScoredCandidate>(library.Count);
        var keywordWeights = SimilarityScorer.PrepareKeywordWeights(options, keywordCounts);
        var likedSourceEmbeddings = profileInputs.LikedSourceEmbeddings;

        if (!likedSourceEmbeddings.IsEmpty)
        {
            var builder = likedSourceEmbeddings.ToBuilder();
            foreach (var entry in library)
            {
                if (builder.ContainsKey(entry.Id) && IsUsable([.. entry.Features.PlotEmbedding]))
                {
                    builder[entry.Id] = entry.Features.PlotEmbedding;
                }
            }

            likedSourceEmbeddings = builder.ToImmutable();
        }

        foreach (var entry in library)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Id == selectedMovieId || entry.Review.IsDisliked)
            {
                continue;
            }

            var features = entry.Features;
            var embedding = features.PlotEmbedding;
            var genres = RecommendationScoring.Genres(features);
            var similarity = SimilarityScorer.Score(
                selectedFeatures,
                features,
                options,
                genreWeights,
                keywordWeights);

            var tier = Classify(similarity, options.MinimumSimilarity, strongReasonCriteria);
            if (tier == MatchTier.None)
            {
                continue;
            }

            var profileSimilarity = ProfileSimilarityCalculator.Evaluate(
                genres,
                embedding,
                profile,
                genreWeights);

            var reviewAffinity = ProfileSimilarityCalculator.EvaluateReviewAffinity(
                genres,
                RecommendationScoring.Cast(features),
                RecommendationScoring.Directors(features),
                embedding,
                profile,
                likedSourceEmbeddings,
                options.Weights,
                genreWeights);

            var contribution = RecommendationScoring.ProfileContribution(
                profileSimilarity.Similarity,
                reviewAffinity,
                options);

            var score = BlendScorer.ScoreWithProfileContribution(
                similarity.Score,
                contribution,
                entry.IsWatched ? 1.0 : 0.0,
                0.0,
                options);

            scored.Add(new ScoredCandidate(
                entry,
                features,
                new SimilarityResult(
                    score,
                    BuildReasons(
                        similarity.Reasons,
                        profileSimilarity,
                        reviewAffinity,
                        contribution,
                        options)),
                tier));
        }

        return scored;
    }

    /// <summary>
    /// Classifies a raw DEV-518 pair result before taste blend and the watched penalty. Below-bar candidates
    /// qualify only when an enabled configured strong-reason criterion is exactly 1.0.
    /// </summary>
    private static MatchTier Classify(
        SimilarityResult similarity,
        double minimumSimilarity,
        IReadOnlySet<string> strongReasonCriteria)
    {
        if (similarity.Score >= minimumSimilarity)
        {
            return MatchTier.AboveBar;
        }

        if (strongReasonCriteria.Count == 0)
        {
            return MatchTier.None;
        }

        foreach (var contribution in similarity.Breakdown)
        {
            if (contribution.Weight > 0.0
                && contribution.Value == 1.0
                && strongReasonCriteria.Contains(contribution.Name))
            {
                return MatchTier.StrongReason;
            }
        }

        return MatchTier.None;
    }

    private static ImmutableList<Reason> BuildReasons(
        ImmutableList<Reason> structuralReasons,
        ProfileSimilarityEvidence profileSimilarity,
        ReviewAffinityEvidence reviewAffinity,
        double profileContribution,
        RecommendationOptions options)
    {
        var builder = structuralReasons.ToBuilder();

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

    private enum MatchTier
    {
        None = 0,
        AboveBar = 1,
        StrongReason = 2,
    }

    private readonly record struct PartitionedLibrary(
        List<RecommendationLibraryEntry> Eligible,
        bool SelectedInLoad,
        float[]? SelectedVector);

    private readonly record struct ScoredCandidate(
        RecommendationLibraryEntry Library,
        MovieFeatures Features,
        SimilarityResult Result,
        MatchTier Tier);
}

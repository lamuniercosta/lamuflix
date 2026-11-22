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
        if (IsEmptySelection(partitioned))
        {
            return ImmutableList<RecommendedMovie>.Empty;
        }

        var context = await LoadScoringContextAsync(query, partitioned.SelectedInLoad, cancellationToken);

        var selectedVector = ResolveSelectedVector(partitioned, context.ProfileInputs, query.MovieId);
        if (selectedVector is null)
        {
            return ImmutableList<RecommendedMovie>.Empty;
        }

        var selectedFeatures = MovieFeaturesMapper.From(
            selected,
            includeSignals: true,
            plotEmbedding: selectedVector);

        var strongReasonCriteria = new HashSet<string>(
            recommendationOptions.StrongReasonCriteria ?? [],
            System.StringComparer.OrdinalIgnoreCase);

        var scored = Score(
            selectedFeatures,
            library,
            query.MovieId,
            strongReasonCriteria,
            context.GenreWeights,
            context.KeywordCounts,
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
            context.GenreWeights,
            context.KeywordCounts,
            relevanceOffset);
        var strongReasonBudget = System.Math.Max(0, query.Take - selectedIds.Count);
        var strongReasonIds = SelectTier(
            strongReason,
            strongReasonBudget,
            selectedFeatures,
            recommendationOptions,
            context.GenreWeights,
            context.KeywordCounts,
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
                    SimilarityScorer.BuildEvidence(item.Result, recommendationOptions))),
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

    private static bool IsEmptySelection(PartitionedLibrary partitioned)
    {
        if (partitioned.Eligible.Count == 0)
        {
            return true;
        }

        return partitioned.SelectedVector is { } selectedVector && !IsUsable(selectedVector);
    }

    private async Task<ScoringContext> LoadScoringContextAsync(
        GetRecommendationsQuery query,
        bool selectedInLoad,
        CancellationToken cancellationToken)
    {
        var genreWeights = await genreWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var keywordCounts = await keywordWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var profileInputs = await profileSource.GetProfileInputsAsync(
            selectedInLoad ? [] : [query.MovieId],
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return new ScoringContext(genreWeights, keywordCounts, profileInputs);
    }

    private static float[]? ResolveSelectedVector(
        PartitionedLibrary partitioned,
        RecommendationProfileInputs profileInputs,
        MovieId selectedMovieId)
    {
        if (partitioned.SelectedVector is not null)
        {
            return partitioned.SelectedVector;
        }

        var fallback = new List<float>(profileInputs.CandidateEmbeddings.GetValueOrDefault(
            selectedMovieId,
            ImmutableArray<float>.Empty));
        return IsUsable([.. fallback]) ? [.. fallback] : null;
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
        IReadOnlySet<string> strongReasonCriteria,
        GenreWeights genreWeights,
        KeywordCounts keywordCounts,
        RecommendationOptions options,
        CancellationToken cancellationToken)
    {
        var scored = new List<ScoredCandidate>(library.Count);
        var keywordWeights = SimilarityScorer.PrepareKeywordWeights(options, keywordCounts);
        var scoring = new EntryScoring(selectedFeatures, strongReasonCriteria, genreWeights, keywordWeights, options);

        foreach (var entry in library)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (scoring.TryScore(entry, selectedMovieId) is { } scoredCandidate)
            {
                scored.Add(scoredCandidate);
            }
        }

        return scored;
    }

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
            if (IsStrongReason(contribution, strongReasonCriteria))
            {
                return MatchTier.StrongReason;
            }
        }

        return MatchTier.None;
    }

    private static bool IsStrongReason(CriterionContribution contribution, IReadOnlySet<string> strongReasonCriteria)
    {
        if (contribution.Weight <= 0.0 || contribution.Value < 1.0)
        {
            return false;
        }

        return strongReasonCriteria.Contains(contribution.Name);
    }

    private sealed class EntryScoring(
        MovieFeatures selectedFeatures,
        IReadOnlySet<string> strongReasonCriteria,
        GenreWeights genreWeights,
        IReadOnlyDictionary<string, double> keywordWeights,
        RecommendationOptions options)
    {
        public ScoredCandidate? TryScore(RecommendationLibraryEntry entry, MovieId selectedMovieId)
        {
            if (entry.Id == selectedMovieId || entry.Review.IsDisliked)
            {
                return null;
            }

            var similarity = SimilarityScorer.Score(
                selectedFeatures,
                entry.Features,
                options,
                genreWeights,
                keywordWeights);

            var tier = Classify(similarity, options.MinimumSimilarity, strongReasonCriteria);
            if (tier == MatchTier.None)
            {
                return null;
            }

            return FinishScore(entry, similarity, tier);
        }

        private ScoredCandidate FinishScore(
            RecommendationLibraryEntry entry,
            SimilarityResult similarity,
            MatchTier tier)
        {
            var score = BlendScorer.Score(
                similarity.Score,
                entry.IsWatched ? 1.0 : 0.0,
                options);

            return new ScoredCandidate(
                entry,
                entry.Features,
                new SimilarityResult(score, similarity.Reasons) { Breakdown = similarity.Breakdown },
                tier);
        }
    }

    private enum MatchTier
    {
        None = 0,
        AboveBar = 1,
        StrongReason = 2,
    }

    private readonly record struct ScoringContext(
        GenreWeights GenreWeights,
        KeywordCounts KeywordCounts,
        RecommendationProfileInputs ProfileInputs);

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

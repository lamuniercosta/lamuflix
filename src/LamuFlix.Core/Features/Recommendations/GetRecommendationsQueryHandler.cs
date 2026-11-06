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

        var selectedFeatures = MovieFeaturesMapper.From(selected, includeSignals: true);
        var pool = await candidates.GetCandidatesAsync(
            query.MovieId,
            selectedFeatures.CollectionId,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var eligible = ExcludeDisliked(pool, cancellationToken);

        if (eligible.Count == 0)
        {
            return ImmutableList<RecommendedMovie>.Empty;
        }

        var genreWeights = await genreWeightsCalculator.GetAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var profile = await profileSource.GetTasteProfileAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var profileInputs = await profileSource.GetProfileInputsAsync(
            [.. eligible.Select(candidate => candidate.Movie.Id)],
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var recommendationOptions = options.Value;
        var scored = Score(
            selectedFeatures,
            eligible,
            profile,
            profileInputs.CandidateEmbeddings,
            genreWeights,
            recommendationOptions,
            cancellationToken);

        var relevanceOffset = BlendScorer.NonNegativeOffset(scored.Select(item => item.Result.Score));
        var mmrCandidates = scored
            .Select(item => new MmrCandidate(item.Movie.Id, item.Result.Score + relevanceOffset, item.Features))
            .ToList();
        var selectedIds = MmrSelector.Select(
            mmrCandidates,
            query.Take,
            recommendationOptions.MmrLambda,
            recommendationOptions.Weights,
            genreWeights);

        var groupedIds = SeedCollectionGrouper.Group(
            mmrCandidates,
            selectedIds,
            selectedFeatures,
            query.Take,
            recommendationOptions.Weights,
            genreWeights);

        var byId = scored.ToDictionary(item => item.Movie.Id);

        return
        [
            .. groupedIds
                .Select(id => byId[id])
                .Select(item => new RecommendedMovie(
                    MovieSummaryMapper.From(item.Movie),
                    item.Result.Score,
                    item.Result.Reasons)),
        ];
    }


    private static List<RecommendationCandidate> ExcludeDisliked(
        IReadOnlyList<RecommendationCandidate> pool,
        CancellationToken cancellationToken)
    {
        var eligible = new List<RecommendationCandidate>(pool.Count);

        foreach (var candidate in pool)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!candidate.Movie.Review.IsDisliked)
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

            var features = MovieFeaturesMapper.From(candidate.Movie, includeSignals: true);
            var genres = RecommendationScoring.Genres(features);
            var embedding = candidateEmbeddings.GetValueOrDefault(
                candidate.Movie.Id,
                ImmutableArray<float>.Empty);

            var similarity = SimilarityScorer.Score(
                selectedFeatures,
                features,
                options.Weights,
                genreWeights);

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
                options.Weights,
                genreWeights);

            var contribution = RecommendationScoring.ProfileContribution(
                profileSimilarity.Similarity,
                reviewAffinity,
                options);

            var score = BlendScorer.ScoreWithProfileContribution(
                similarity.Score,
                contribution,
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
                features,
                new SimilarityResult(
                    score,
                    BuildReasons(
                        similarity.Reasons,
                        profileSimilarity,
                        reviewAffinity,
                        contribution,
                        options))));
        }

        return scored;
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





    private readonly record struct ScoredCandidate(
        Movie Movie,
        MovieFeatures Features,
        SimilarityResult Result);
}

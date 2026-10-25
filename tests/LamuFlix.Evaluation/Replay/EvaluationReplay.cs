using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Evaluation.Fixtures;
using LamuFlix.Evaluation.Metrics;

namespace LamuFlix.Evaluation.Replay;

/// <summary>
/// T009 — strict as-of replay over the real DEV-416 final MMR stage
/// (<see cref="MmrSelector.Select"/>, reconciled in TB002). No replacement
/// ranker or fake ranking path: the declared lambda, similarity weights and
/// catalog-derived genre weights are passed straight through, and the returned
/// MMR selection order is consumed without re-sorting.
/// </summary>
public static class EvaluationReplay
{
    public static EvaluationReplayResult Run(EvaluationFixture fixture, EvaluationReplayOptions options)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(options);

        var orderedEvents = fixture.Events
            .OrderBy(observation => observation.Timestamp)
            .ThenBy(observation => observation.EventId, StringComparer.Ordinal)
            .ToImmutableArray();
        var features = fixture.Catalog.ToDictionary(movie => movie.MovieId, movie => movie.Features);
        ImmutableArray<MovieId> canonicalIds =
            [.. fixture.Catalog.Select(movie => movie.MovieId).OrderBy(movieId => movieId.Value)];
        var genreWeights = AsOfHistory.CatalogGenreWeights(fixture.Catalog);
        var random = new Random(options.Seed);

        var observations = ImmutableArray.CreateBuilder<EvaluationObservationResult>(orderedEvents.Length);
        foreach (var target in orderedEvents)
        {
            observations.Add(
                Score(fixture, orderedEvents, target, options, features, canonicalIds, genreWeights, random));
        }

        var results = observations.ToImmutable();
        var metrics = MetricCalculator.Compute(
            [.. results.Select(result => new EvaluationObservation(result.Target, result.Ranking))]);

        return new EvaluationReplayResult(options.Seed, results, metrics);
    }

    private static EvaluationObservationResult Score(
        EvaluationFixture fixture,
        ImmutableArray<EvaluationEvent> orderedEvents,
        EvaluationEvent target,
        EvaluationReplayOptions options,
        Dictionary<MovieId, MovieFeatures> features,
        ImmutableArray<MovieId> canonicalIds,
        GenreWeights genreWeights,
        Random random)
    {
        var clock = options.ObservationClock(target.Timestamp)
            ?? throw new InvalidOperationException("The observation clock factory returned null.");
        var scores = AsOfHistory.RelevanceScores(fixture.Catalog, orderedEvents, target.Timestamp, clock);
        var offset = RelevanceOffset(scores);

        var candidates = ShuffledPool(canonicalIds, random)
            .Select(movieId => new MmrCandidate(movieId, scores[movieId] + offset, features[movieId]))
            .ToImmutableArray();

        var ranking = MmrSelector.Select(
            candidates,
            MetricCalculator.Cutoff,
            fixture.RankingInputs.Lambda,
            fixture.RankingInputs.SimilarityWeights,
            genreWeights);

        return new EvaluationObservationResult(
            target.EventId,
            target.UserId,
            target.MovieId,
            target.Timestamp,
            [.. ranking],
            scores[target.MovieId]);
    }

    private static double RelevanceOffset(ImmutableDictionary<MovieId, double> scores)
    {
        if (scores.IsEmpty)
        {
            return 0.0;
        }

        var minimum = scores.Values.Min();
        return minimum < 0.0 ? -minimum : 0.0;
    }

    /// <summary>
    /// TB002 requires a seeded <see cref="Random"/> reset per evaluation. Candidate
    /// pool order is the only randomizable preparation input; <see cref="MmrSelector"/>
    /// is proven input-order independent, so selection and metrics stay deterministic.
    /// </summary>
    private static ImmutableArray<MovieId> ShuffledPool(ImmutableArray<MovieId> canonicalIds, Random random)
    {
        var pool = canonicalIds.ToArray();

        for (var index = pool.Length - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (pool[index], pool[swapIndex]) = (pool[swapIndex], pool[index]);
        }

        return [.. pool];
    }
}

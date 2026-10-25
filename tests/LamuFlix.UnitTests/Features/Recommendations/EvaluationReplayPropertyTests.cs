using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Evaluation.Fixtures;
using LamuFlix.Evaluation.Metrics;
using LamuFlix.Evaluation.Replay;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class EvaluationReplayPropertyTests
{
    private const double Tolerance = 1e-12;

    [Property(MaxTest = 60, Replay = "356101,200003")]
    [Trait("Category", "Property")]
    public bool Run_GeneratedFixtures_ProduceMetricsWithinUnitInterval(
        int catalogSeed,
        int[] eventSeeds,
        int lambdaSeed)
    {
        // arrange
        var fixture = EvaluationPropertyFixtures.Fixture(catalogSeed, eventSeeds, lambdaSeed);

        // act
        var metrics = Replay(fixture).Metrics;

        // assert
        return metrics.ObservationCount == fixture.Events.Length
            && IsUnitInterval(metrics.HitRateAt10)
            && IsUnitInterval(metrics.Mrr);
    }

    private static bool IsUnitInterval(double value) =>
        double.IsFinite(value)
        && value >= 0.0
        && value <= 1.0;

    [Property(MaxTest = 60, Replay = "356102,300001")]
    [Trait("Category", "Property")]
    public bool Run_GeneratedFixtures_OrderObservationsByTimestampThenOrdinalEventId(
        int catalogSeed,
        int[] eventSeeds,
        int lambdaSeed)
    {
        // arrange
        var fixture = EvaluationPropertyFixtures.Fixture(catalogSeed, eventSeeds, lambdaSeed);

        // act
        var observations = Replay(fixture).Observations;

        // assert
        return observations.Select(observation => observation.EventId)
            .SequenceEqual(ExpectedOrder(fixture.Events));
    }

    [Property(MaxTest = 60, Replay = "356103,400001")]
    [Trait("Category", "Property")]
    public bool Run_ReversedEventPresentation_LeavesResultsIdentical(
        int catalogSeed,
        int[] eventSeeds,
        int lambdaSeed)
    {
        // arrange
        var fixture = EvaluationPropertyFixtures.Fixture(catalogSeed, eventSeeds, lambdaSeed);
        var permuted = fixture with { Events = [.. fixture.Events.Reverse()] };

        // act
        var baseline = Replay(fixture);
        var candidate = Replay(permuted);

        // assert
        return SameResults(baseline.Observations, candidate.Observations)
            && SameMetrics(baseline.Metrics, candidate.Metrics);
    }

    [Property(MaxTest = 60, Replay = "356104,500009")]
    [Trait("Category", "Property")]
    public bool Run_AppendedFutureEvents_LeaveEveryEarlierObservationUnchanged(
        int catalogSeed,
        int[] eventSeeds,
        int lambdaSeed)
    {
        // arrange
        var fixture = EvaluationPropertyFixtures.Fixture(catalogSeed, eventSeeds, lambdaSeed);
        var future = EvaluationPropertyFixtures.FutureEvents(fixture, lambdaSeed);
        var extended = fixture with { Events = [.. fixture.Events, .. future] };

        // act
        var baseline = Replay(fixture);
        var candidate = Replay(extended);

        // assert
        return MatchesPrefix(baseline.Observations, candidate.Observations)
            && SameMetrics(baseline.Metrics, PrefixMetrics(candidate, baseline.Observations.Length));
    }

    [Property(MaxTest = 60, Replay = "356105,600007")]
    [Trait("Category", "Property")]
    public bool Run_OrderedEventPrefix_ReproducesThatPrefixOfTheFullReplay(
        int catalogSeed,
        int[] eventSeeds,
        int lambdaSeed)
    {
        // arrange
        var fixture = EvaluationPropertyFixtures.Fixture(catalogSeed, eventSeeds, lambdaSeed);
        var orderedIds = ExpectedOrder(fixture.Events);
        var prefixLength = orderedIds.Length == 0
            ? 0
            : (int)((uint)lambdaSeed % (uint)(orderedIds.Length + 1));
        var prefixIds = orderedIds.Take(prefixLength).ToHashSet(StringComparer.Ordinal);
        var prefixFixture = fixture with
        {
            Events =
            [
                .. fixture.Events
                    .Where(evaluationEvent => prefixIds.Contains(evaluationEvent.EventId))
                    .OrderBy(evaluationEvent => evaluationEvent.Timestamp)
                    .ThenBy(evaluationEvent => evaluationEvent.EventId, StringComparer.Ordinal),
            ],
        };

        // act
        var full = Replay(fixture);
        var prefix = Replay(prefixFixture);

        // assert
        return prefix.Observations.Length == prefixLength
            && MatchesPrefix([.. full.Observations.Take(prefixLength)], prefix.Observations);
    }

    private static EvaluationReplayResult Replay(EvaluationFixture fixture) =>
        EvaluationReplay.Run(fixture, EvaluationReplayOptions.Default(356));

    private static ImmutableArray<string> ExpectedOrder(IEnumerable<EvaluationEvent> events) =>
        [.. events
            .OrderBy(evaluationEvent => evaluationEvent.Timestamp)
            .ThenBy(evaluationEvent => evaluationEvent.EventId, StringComparer.Ordinal)
            .Select(evaluationEvent => evaluationEvent.EventId)];

    private static bool MatchesPrefix(
        IReadOnlyList<EvaluationObservationResult> expected,
        IReadOnlyList<EvaluationObservationResult> actual) =>
        actual.Count >= expected.Count
        && Enumerable.Range(0, expected.Count)
            .All(index => SameObservation(expected[index], actual[index]));

    private static bool SameResults(
        IReadOnlyList<EvaluationObservationResult> expected,
        IReadOnlyList<EvaluationObservationResult> actual) =>
        expected.Count == actual.Count
        && MatchesPrefix(expected, actual);

    private static bool SameObservation(
        EvaluationObservationResult expected,
        EvaluationObservationResult actual) =>
        string.Equals(expected.EventId, actual.EventId, StringComparison.Ordinal)
        && string.Equals(expected.UserId, actual.UserId, StringComparison.Ordinal)
        && expected.Target == actual.Target
        && expected.Timestamp == actual.Timestamp
        && expected.Ranking.SequenceEqual(actual.Ranking)
        && expected.Score.Equals(actual.Score);

    private static bool SameMetrics(EvaluationMetrics expected, EvaluationMetrics actual) =>
        expected.ObservationCount == actual.ObservationCount
        && expected.HitCount == actual.HitCount
        && Math.Abs(expected.HitRateAt10 - actual.HitRateAt10) <= Tolerance
        && Math.Abs(expected.ReciprocalRankSum - actual.ReciprocalRankSum) <= Tolerance
        && Math.Abs(expected.Mrr - actual.Mrr) <= Tolerance;

    private static EvaluationMetrics PrefixMetrics(EvaluationReplayResult result, int prefixLength) =>
        MetricCalculator.Compute(
        [
            .. result.Observations
                .Take(prefixLength)
                .Select(observation => new EvaluationObservation(observation.Target, observation.Ranking)),
        ]);
}

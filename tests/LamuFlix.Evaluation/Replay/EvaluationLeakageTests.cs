using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Evaluation.Fixtures;
using LamuFlix.Evaluation.Metrics;
using LamuFlix.Evaluation.Replay;
using Shouldly;

namespace LamuFlix.Evaluation.Tests.Replay;

/// <summary>
/// T008 — temporal leakage isolation. Every earlier observation's ranking and
/// score is compared, not just aggregate metrics. Publication is out of scope
/// here; equality is proven at the in-memory replay/metrics layer.
/// </summary>
public sealed class EvaluationLeakageTests
{
    private static readonly EvaluationEvent[] BaselineEvents =
    [
        ReplayFixtureFactory.Event("e1", "u1", 1, dayOffset: 0),
        ReplayFixtureFactory.Event("e2", "u2", 2, dayOffset: 1),
        ReplayFixtureFactory.Event("e3", "u1", 2, dayOffset: 2),
    ];

    private static readonly EvaluationEvent[] FutureEvents =
    [
        ReplayFixtureFactory.Event("f1", "u1", 3, dayOffset: 3),
        ReplayFixtureFactory.Event("f2", "u2", 3, dayOffset: 4),
        ReplayFixtureFactory.Event("f3", "u3", 1, dayOffset: 5),
    ];

    [Fact]
    public void Run_FutureEventsAppended_LeavesEarlierObservationRankingsAndScoresUnchanged()
    {
        // arrange
        var baseline = Run(BaselineEvents);

        // act
        var appended = Run([.. BaselineEvents, .. FutureEvents]);

        // assert
        ShouldMatchBaseline(baseline, appended);
    }

    [Fact]
    public void Run_FutureEventsShuffled_LeavesEarlierObservationRankingsAndScoresUnchanged()
    {
        // arrange
        var baseline = Run(BaselineEvents);

        // act
        var shuffled = Run(
        [
            FutureEvents[2],
            BaselineEvents[1],
            FutureEvents[0],
            BaselineEvents[2],
            FutureEvents[1],
            BaselineEvents[0],
        ]);

        // assert
        ShouldMatchBaseline(baseline, shuffled);
    }

    [Fact]
    public void Run_TargetEventIsExcludedFromItsOwnHistory()
    {
        // arrange: the same movie is played twice; the second play only sees the first
        var fixture = ReplayFixtureFactory.Fixture(
        [
            ReplayFixtureFactory.Event("e1", "u1", 1, dayOffset: 0),
            ReplayFixtureFactory.Event("e2", "u1", 1, dayOffset: 1),
        ]);

        // act
        var result = EvaluationReplay.Run(fixture, ReplayFixtureFactory.Options());

        // assert
        result.Observations[0].Score.ShouldBe(0.0);
        result.Observations[1].Score.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public void Run_EqualTimeFutureEvents_DoNotEnterEarlierObservationHistory()
    {
        // arrange
        var baseline = Run(BaselineEvents);
        var equalTime = ReplayFixtureFactory.Event("x1", "u9", 3, dayOffset: 2);

        // act
        var withEqualTime = Run([.. BaselineEvents, equalTime]);

        // assert
        ShouldMatchBaseline(baseline, withEqualTime);
    }

    [Fact]
    public void Run_CrossUserFutureHistory_DoesNotLeakIntoEarlierObservations()
    {
        // arrange
        var baseline = Run(BaselineEvents);
        EvaluationEvent[] crossUserFuture =
        [
            ReplayFixtureFactory.Event("f1", "u3", 3, dayOffset: 3),
            ReplayFixtureFactory.Event("f2", "u3", 1, dayOffset: 4),
            ReplayFixtureFactory.Event("f3", "u4", 2, dayOffset: 5),
        ];

        // act
        var withCrossUserFuture = Run([.. BaselineEvents, .. crossUserFuture]);

        // assert
        ShouldMatchBaseline(baseline, withCrossUserFuture);
    }

    [Fact]
    public void Run_FutureDerivedPopularityBurst_DoesNotLeakIntoEarlierObservations()
    {
        // arrange
        var baseline = Run(BaselineEvents);
        var burst = Enumerable.Range(0, 10)
            .Select(index => ReplayFixtureFactory.Event($"b{index}", $"x{index}", 2, dayOffset: 3 + index));

        // act
        var withBurst = Run([.. BaselineEvents, .. burst]);

        // assert: movie 2 would dominate any future-derived profile, but earlier as-of scores cannot move
        ShouldMatchBaseline(baseline, withBurst);
    }

    [Fact]
    public void Run_PermutedFixturePresentation_ProducesIdenticalInMemoryResults()
    {
        // arrange: same catalog/events declared in a different presentation order
        EvaluationCatalogMovie[] catalog =
        [
            ReplayFixtureFactory.Movie(3),
            ReplayFixtureFactory.Movie(1, "Drama"),
            ReplayFixtureFactory.Movie(2, "Drama"),
        ];
        EvaluationCatalogMovie[] permutedCatalog =
        [
            ReplayFixtureFactory.Movie(2, "Drama"),
            ReplayFixtureFactory.Movie(3),
            ReplayFixtureFactory.Movie(1, "Drama"),
        ];
        EvaluationEvent[] complete = [.. BaselineEvents, .. FutureEvents];

        var original = EvaluationReplay.Run(
            ReplayFixtureFactory.Fixture(complete, catalog),
            ReplayFixtureFactory.Options());

        // act
        var permuted = EvaluationReplay.Run(
            ReplayFixtureFactory.Fixture([.. Enumerable.Reverse(complete)], permutedCatalog),
            ReplayFixtureFactory.Options());

        // assert
        permuted.Observations.SequenceEqual(original.Observations, ResultComparer.Instance).ShouldBeTrue();
        permuted.Metrics.ShouldBe(original.Metrics);
    }

    private static EvaluationReplayResult Run(IEnumerable<EvaluationEvent> events) =>
        EvaluationReplay.Run(
            ReplayFixtureFactory.Fixture(events),
            ReplayFixtureFactory.Options());

    private static void ShouldMatchBaseline(EvaluationReplayResult baseline, EvaluationReplayResult candidate)
    {
        var expectedById = baseline.Observations.ToDictionary(observation => observation.EventId, StringComparer.Ordinal);

        foreach (var (eventId, expected) in expectedById)
        {
            var actual = candidate.Observations.Single(observation => string.Equals(
                observation.EventId,
                eventId,
                StringComparison.Ordinal));

            actual.ShouldSatisfyAllConditions(
                () => actual.EventId.ShouldBe(expected.EventId),
                () => ReplayFixtureFactory.Ids(actual.Ranking).ShouldBe(ReplayFixtureFactory.Ids(expected.Ranking)),
                () => actual.Score.ShouldBe(expected.Score));
        }

        MetricsOf(candidate, [.. expectedById.Keys]).ShouldBe(baseline.Metrics);
    }

    private static EvaluationMetrics MetricsOf(EvaluationReplayResult result, params string[] eventIds) =>
        MetricCalculator.Compute(
        [
            .. result.Observations
                .Where(observation => eventIds.Contains(observation.EventId, StringComparer.Ordinal))
                .Select(observation => new EvaluationObservation(observation.Target, observation.Ranking)),
        ]);
}

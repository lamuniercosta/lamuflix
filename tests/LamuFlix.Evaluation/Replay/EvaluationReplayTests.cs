using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Evaluation.Fixtures;

namespace LamuFlix.Evaluation.Replay;

public sealed class EvaluationReplayTests
{
    private static readonly EvaluationEvent[] HistoryEvents =
    [
        ReplayFixtureFactory.Event("e1", "u1", 1, dayOffset: 0),
        ReplayFixtureFactory.Event("e2", "u2", 2, dayOffset: 1),
        ReplayFixtureFactory.Event("e3", "u1", 3, dayOffset: 2),
    ];

    [Fact]
    public void Run_OrdersObservationsByTimestampThenOrdinalEventId()
    {
        // arrange
        var fixture = ReplayFixtureFactory.Fixture(
        [
            ReplayFixtureFactory.Event("e5", "u1", 1, dayOffset: 2),
            ReplayFixtureFactory.Event("eB", "u1", 2, dayOffset: 1),
            ReplayFixtureFactory.Event("eA", "u1", 3, dayOffset: 1),
            ReplayFixtureFactory.Event("e1", "u1", 1, dayOffset: 0),
        ]);

        // act
        var result = EvaluationReplay.Run(fixture, ReplayFixtureFactory.Options());

        // assert
        result.Observations.Select(observation => observation.EventId).ShouldBe(["e1", "eA", "eB", "e5"]);
    }

    [Fact]
    public void Run_HistoryUsesOnlyStrictlyEarlierEventsAcrossUsers()
    {
        // act
        var result = EvaluationReplay.Run(
            ReplayFixtureFactory.Fixture(HistoryEvents),
            ReplayFixtureFactory.Options());

        // assert
        var second = result.Observations[1];
        ReplayFixtureFactory.Ids(second.Ranking).ShouldBe([1, 2, 3]);
        second.Score.ShouldBe(0.0);

        var third = result.Observations[2];
        ReplayFixtureFactory.Ids(third.Ranking).ShouldBe([2, 1, 3]);
    }

    [Fact]
    public void Run_EqualTimeEventsCannotTeachOneAnother()
    {
        // arrange
        var fixture = ReplayFixtureFactory.Fixture(
        [
            ReplayFixtureFactory.Event("e1", "u1", 1, dayOffset: 0),
            ReplayFixtureFactory.Event("e2", "u2", 2, dayOffset: 0),
        ]);

        // act
        var result = EvaluationReplay.Run(fixture, ReplayFixtureFactory.Options());

        // assert
        foreach (var observation in result.Observations)
        {
            ReplayFixtureFactory.Ids(observation.Ranking).ShouldBe([1, 2, 3]);
            observation.Score.ShouldBe(0.0);
        }
    }

    [Fact]
    public void Run_ScoresEarlierObservationBeforeLaterEventsBecomeVisible()
    {
        // arrange
        var fixture = ReplayFixtureFactory.Fixture(HistoryEvents);
        var options = ReplayFixtureFactory.Options();
        var full = EvaluationReplay.Run(fixture, options);

        // act and assert
        for (var prefixLength = 1; prefixLength <= HistoryEvents.Length; prefixLength++)
        {
            var prefix = EvaluationReplay.Run(
                ReplayFixtureFactory.Fixture(HistoryEvents.Take(prefixLength)),
                options);

            prefix.Observations.SequenceEqual(full.Observations.Take(prefixLength), ResultComparer.Instance)
                .ShouldBeTrue($"prefix of {prefixLength} observation(s) must match the full replay");
        }
    }

    [Fact]
    public void Run_InjectedObservationClock_IsFixedToEachTargetTimestamp()
    {
        // arrange
        var observedClocks = new List<DateTimeOffset>();
        var options = new EvaluationReplayOptions(
            356,
            target =>
            {
                observedClocks.Add(target);
                return new FixedUtcTimeProvider(target);
            });

        // act
        var result = EvaluationReplay.Run(ReplayFixtureFactory.Fixture(HistoryEvents), options);

        // assert
        observedClocks.ShouldBe(result.Observations.Select(observation => observation.Timestamp));
        observedClocks.ShouldAllBe(timestamp => timestamp.Offset == TimeSpan.Zero);
    }

    [Fact]
    public void Run_FixedSeedAndClock_ProducesIdenticalResults()
    {
        // arrange
        var fixture = ReplayFixtureFactory.Fixture(HistoryEvents);

        // act
        var first = EvaluationReplay.Run(fixture, ReplayFixtureFactory.Options());
        var second = EvaluationReplay.Run(fixture, ReplayFixtureFactory.Options());

        // assert
        first.Seed.ShouldBe(356);
        first.Observations.SequenceEqual(second.Observations, ResultComparer.Instance).ShouldBeTrue();
        first.Metrics.ShouldBe(second.Metrics);
    }

    [Fact]
    public void Run_DifferentSeeds_ProduceIdenticalSelectionResults()
    {
        // arrange
        var fixture = ReplayFixtureFactory.Fixture(HistoryEvents);

        // act
        var first = EvaluationReplay.Run(fixture, ReplayFixtureFactory.Options());
        var second = EvaluationReplay.Run(fixture, ReplayFixtureFactory.Options(999));

        // assert
        first.Observations.SequenceEqual(second.Observations, ResultComparer.Instance).ShouldBeTrue();
        second.Seed.ShouldBe(999);
    }
}

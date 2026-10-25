using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using LamuFlix.Core.Domain;
using LamuFlix.Evaluation.Fixtures;
using LamuFlix.Evaluation.Metrics;
using LamuFlix.Evaluation.Replay;
using Shouldly;

namespace LamuFlix.Evaluation.Tests.Replay;

/// <summary>
/// T011/T012 — real DEV-416 fixed-fixture integration on the nonempty baseline.
/// The expected per-observation rankings and HR@10/MRR are independently worked
/// from the Q4/Q5 semantics and recorded in
/// <c>Fixtures/baseline.expected.json</c> before this test ever ran.
/// </summary>
public sealed class RealRankingBaselineTests
{
    private static readonly int Seed = EvaluationBaselineFixture.DefaultSeed;

    [Fact]
    public void LoadFromJson_BaselineFixture_IsNonEmptyAndDeclaresImmutableInputs()
    {
        // act
        var fixture = EvaluationBaselineFixture.Load();

        // assert
        fixture.ShouldSatisfyAllConditions(
            f => f.FixtureId.ShouldBe("dev-356-baseline"),
            f => f.Catalog.Length.ShouldBe(12),
            f => f.Events.Length.ShouldBe(12),
            f => f.RankingInputs.Lambda.ShouldBe(0.5),
            f => f.Events.Select(observation => observation.EventId).Distinct().Count().ShouldBe(12));
    }

    [Fact]
    public void Run_BaselineFixture_ThroughRealRanking_MatchesIndependentlyWorkedRankings()
    {
        // arrange
        var expected = EvaluationBaselineFixture.LoadExpected();
        var fixture = EvaluationBaselineFixture.Load();

        // act
        var result = EvaluationReplay.Run(fixture, EvaluationReplayOptions.Default(Seed));

        // assert
        result.Observations.Select(observation => observation.EventId)
            .ShouldBe(expected.Observations.Select(observation => observation.EventId));

        foreach (var (observation, index) in result.Observations.Select(
                     (observation, index) => (observation, index)))
        {
            var worked = expected.Observations[index];

            observation.Target.Value.ShouldBe(worked.Target);
            EvaluationBaselineFixture.Ids(observation.Ranking).ShouldBe(worked.Ranking);
            EvaluationBaselineFixture.RankOf(observation).ShouldBe(worked.Rank);
        }
    }

    [Fact]
    public void Run_BaselineFixture_ThroughRealRanking_MatchesIndependentlyWorkedMetrics()
    {
        // arrange
        var expected = EvaluationBaselineFixture.LoadExpected();
        var fixture = EvaluationBaselineFixture.Load();

        // act
        var metrics = EvaluationReplay.Run(fixture, EvaluationReplayOptions.Default(Seed)).Metrics;

        // assert
        metrics.ShouldSatisfyAllConditions(
            m => m.ObservationCount.ShouldBe(expected.ObservationCount),
            m => m.HitCount.ShouldBe(expected.HitCount),
            m => m.HitRateAt10.ShouldBe(expected.HitRateAt10),
            m => m.ReciprocalRankSum.ShouldBe(expected.ReciprocalRankSum, EvaluationBaselineFixture.Tolerance),
            m => m.Mrr.ShouldBe(expected.Mrr, EvaluationBaselineFixture.Tolerance),
            m => m.HitRateAt10.ShouldBe((double)expected.HitCount / expected.ObservationCount),
            m => m.Mrr.ShouldBe(expected.ReciprocalRankSum / expected.ObservationCount, EvaluationBaselineFixture.Tolerance));
    }

    [Fact]
    public void Run_BaselineRealRanking_ProducesUniqueIdsAndRejectsDuplicateRankingIds()
    {
        // arrange
        var fixture = EvaluationBaselineFixture.Load();
        var result = EvaluationReplay.Run(fixture, EvaluationReplayOptions.Default(Seed));

        // assert: the real DEV-416 output never repeats a movie id
        foreach (var observation in result.Observations)
        {
            observation.Ranking.Distinct().Count().ShouldBe(observation.Ranking.Length);
        }

        // act: a duplicate final-ranking id must fail, never be silently repaired
        var first = result.Observations[0];
        var duplicated = first with { Ranking = [.. first.Ranking, first.Ranking[0]] };
        var failure = Should.Throw<ArgumentException>(
            () => MetricCalculator.Compute([new EvaluationObservation(duplicated.Target, duplicated.Ranking)]));

        // assert
        failure.Message.ShouldContain("unique");
    }
}

internal static class EvaluationBaselineFixture
{
    public const int DefaultSeed = 356;
    public const double Tolerance = 1e-12;

    public static EvaluationFixture Load() =>
        EvaluationFixtureLoader.LoadFromJson(
            File.ReadAllText(PathFor("baseline.json")),
            new FixedUtcTimeProvider(new DateTimeOffset(2026, 1, 12, 0, 0, 0, TimeSpan.Zero)));

    public static ExpectedBaseline LoadExpected()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(PathFor("baseline.expected.json")));
        var root = document.RootElement;
        var observations = root.GetProperty("observations")
            .EnumerateArray()
            .Select(ReadObservation)
            .ToImmutableArray();
        var metrics = root.GetProperty("expectedMetrics");

        return new ExpectedBaseline(
            observations,
            metrics.GetProperty("observationCount").GetInt32(),
            metrics.GetProperty("hitCount").GetInt32(),
            metrics.GetProperty("hitRateAt10").GetDouble(),
            Rational(metrics.GetProperty("reciprocalRankSum")),
            Rational(metrics.GetProperty("mrr")));
    }

    public static int[] Ids(ImmutableArray<MovieId> ranking) =>
        [.. ranking.Select(movieId => movieId.Value)];

    public static int RankOf(EvaluationObservationResult observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        for (var index = 0; index < observation.Ranking.Length && index < MetricCalculator.Cutoff; index++)
        {
            if (observation.Ranking[index] == observation.Target)
            {
                return index + 1;
            }
        }

        return 0;
    }

    private static ExpectedObservation ReadObservation(JsonElement entry) =>
        new(
            entry.GetProperty("eventId").GetString()!,
            entry.GetProperty("target").GetInt32(),
            entry.GetProperty("rank").GetInt32(),
            [.. entry.GetProperty("ranking").EnumerateArray().Select(item => item.GetInt32())]);

    private static double Rational(JsonElement element) =>
        (double)element.GetProperty("numerator").GetInt32()
        / element.GetProperty("denominator").GetInt32();

    private static string PathFor(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
}

internal sealed record ExpectedBaseline(
    ImmutableArray<ExpectedObservation> Observations,
    int ObservationCount,
    int HitCount,
    double HitRateAt10,
    double ReciprocalRankSum,
    double Mrr);

internal sealed record ExpectedObservation(string EventId, int Target, int Rank, ImmutableArray<int> Ranking);

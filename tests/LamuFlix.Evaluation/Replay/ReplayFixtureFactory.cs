using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Evaluation.Fixtures;

namespace LamuFlix.Evaluation.Replay;

internal static class ReplayFixtureFactory
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static EvaluationEvent Event(
        string eventId,
        string userId,
        int movieId,
        int dayOffset = 0,
        int minuteOffset = 0) =>
        new(
            eventId,
            userId,
            new MovieId(movieId),
            BaseTime.AddDays(dayOffset).AddMinutes(minuteOffset));

    public static EvaluationCatalogMovie Movie(int movieId, params string[] genres) =>
        new(new MovieId(movieId), new MovieFeatures(genres: genres));

    public static EvaluationFixture Fixture(
        IEnumerable<EvaluationEvent> events,
        IEnumerable<EvaluationCatalogMovie>? catalog = null,
        double lambda = 1.0) =>
        new(
            "replay-fixture",
            catalog?.ToImmutableArray() ?? [Movie(1), Movie(2), Movie(3)],
            new EvaluationRankingInputs(lambda, new SimilarityWeights()),
            [.. events]);

    public static EvaluationReplayOptions Options(int seed = 356) => EvaluationReplayOptions.Default(seed);

    public static int[] Ids(ImmutableArray<MovieId> ranking) =>
        [.. ranking.Select(movieId => movieId.Value)];
}

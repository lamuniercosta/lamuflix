using System;

namespace LamuFlix.Evaluation.Replay;

public sealed record EvaluationReplayOptions
{
    public EvaluationReplayOptions(int seed, Func<DateTimeOffset, TimeProvider> observationClock)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seed);
        ArgumentNullException.ThrowIfNull(observationClock);

        Seed = seed;
        ObservationClock = observationClock;
    }

    public int Seed { get; }

    /// <summary>
    /// Supplies the clock fixed to each target observation UTC timestamp, per TB002.
    /// The replay never reads ambient time.
    /// </summary>
    public Func<DateTimeOffset, TimeProvider> ObservationClock { get; }

    public static EvaluationReplayOptions Default(int seed) =>
        new(seed, static target => new FixedUtcTimeProvider(target));
}

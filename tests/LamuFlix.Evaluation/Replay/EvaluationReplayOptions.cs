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

    public Func<DateTimeOffset, TimeProvider> ObservationClock { get; }

    public static EvaluationReplayOptions Default(int seed) =>
        new(seed, static target => new FixedUtcTimeProvider(target));
}

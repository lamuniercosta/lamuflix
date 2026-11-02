using System;

namespace LamuFlix.Evaluation.Replay;

public enum TmdbSignalsMode
{
    Disabled = 0,
    Enabled = 1,
}

public sealed record EvaluationReplayOptions
{
    public EvaluationReplayOptions(
        int seed,
        Func<DateTimeOffset, TimeProvider> observationClock,
        TmdbSignalsMode tmdbSignals = TmdbSignalsMode.Disabled)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seed);
        ArgumentNullException.ThrowIfNull(observationClock);

        Seed = seed;
        ObservationClock = observationClock;
        TmdbSignals = tmdbSignals;
    }

    public int Seed { get; }

    public Func<DateTimeOffset, TimeProvider> ObservationClock { get; }

    public TmdbSignalsMode TmdbSignals { get; }

    public static EvaluationReplayOptions Default(int seed) => Default(seed, null);

    public static EvaluationReplayOptions Default(int seed, string? tmdbSignals) =>
        new(seed, static target => new FixedUtcTimeProvider(target), ParseTmdbSignals(tmdbSignals));

    private static TmdbSignalsMode ParseTmdbSignals(string? value)
    {
        if (string.IsNullOrEmpty(value)
            || string.Equals(value, nameof(TmdbSignalsMode.Disabled), StringComparison.Ordinal))
        {
            return TmdbSignalsMode.Disabled;
        }

        if (string.Equals(value, nameof(TmdbSignalsMode.Enabled), StringComparison.Ordinal))
        {
            return TmdbSignalsMode.Enabled;
        }

        throw new InvalidOperationException(
            $"TMDb signals mode must be exactly '{nameof(TmdbSignalsMode.Disabled)}' or "
            + $"'{nameof(TmdbSignalsMode.Enabled)}'; got '{value}'.");
    }
}

using System;

namespace LamuFlix.Evaluation.Replay;

internal sealed class FixedUtcTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FixedUtcTimeProvider(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Fixed time must be expressed as UTC.", nameof(utcNow));
        }

        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;
}

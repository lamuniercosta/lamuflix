using System;

namespace LamuFlix.UnitTests.Features;

public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        this.utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => utcNow;
}

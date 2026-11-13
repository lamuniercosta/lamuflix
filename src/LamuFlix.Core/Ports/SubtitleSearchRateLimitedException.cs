using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleSearchRateLimitedException : Exception
{
    public SubtitleSearchRateLimitedException()
        : base("The subtitle search was rate-limited by the provider.")
    {
    }

    public SubtitleSearchRateLimitedException(string message)
        : base(message)
    {
    }

    public SubtitleSearchRateLimitedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SubtitleSearchRateLimitedException(TimeSpan? retryAfter)
        : base("The subtitle search was rate-limited by the provider.")
    {
        RetryAfter = retryAfter;
    }

    public TimeSpan? RetryAfter { get; }
}

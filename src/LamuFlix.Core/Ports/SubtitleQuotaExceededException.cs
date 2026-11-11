using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleQuotaExceededException : Exception
{
    public SubtitleQuotaExceededException()
        : base("The subtitle provider quota was exhausted.")
    {
    }

    public SubtitleQuotaExceededException(string message)
        : base(message)
    {
    }

    public SubtitleQuotaExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SubtitleQuotaExceededException(TimeSpan? retryAfter)
        : base("The subtitle provider quota was exhausted.")
    {
        RetryAfter = retryAfter;
    }

    public TimeSpan? RetryAfter { get; }
}

using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleProviderFailureException : Exception
{
    public SubtitleProviderFailureException()
        : base("The subtitle provider returned an unusable response.")
    {
    }

    public SubtitleProviderFailureException(string message)
        : base(message)
    {
    }

    public SubtitleProviderFailureException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

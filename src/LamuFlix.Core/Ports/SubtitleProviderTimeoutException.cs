using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleProviderTimeoutException : Exception
{
    public SubtitleProviderTimeoutException()
        : base("The subtitle provider did not answer within the configured request timeout.")
    {
    }

    public SubtitleProviderTimeoutException(string message)
        : base(message)
    {
    }

    public SubtitleProviderTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

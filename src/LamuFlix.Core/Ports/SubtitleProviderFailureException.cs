using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleProviderFailureException : Exception
{
    public string? RefusedLinkHost { get; private init; }

    public static SubtitleProviderFailureException ForRefusedLinkHost(string host) =>
        new($"The subtitle provider returned a download link on host '{host}', which is not an allowed https host.")
        {
            RefusedLinkHost = host,
        };

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

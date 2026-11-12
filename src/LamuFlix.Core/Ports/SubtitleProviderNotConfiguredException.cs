using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleProviderNotConfiguredException : Exception
{
    public SubtitleProviderNotConfiguredException()
        : base("The subtitle provider acquisition settings are missing or invalid.")
    {
    }

    public SubtitleProviderNotConfiguredException(string message)
        : base(message)
    {
    }

    public SubtitleProviderNotConfiguredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleAcquisitionUnavailableException : Exception
{
    public SubtitleAcquisitionUnavailableException()
        : base("Subtitle acquisition is unavailable for this library.")
    {
    }

    public SubtitleAcquisitionUnavailableException(string message)
        : base(message)
    {
    }

    public SubtitleAcquisitionUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

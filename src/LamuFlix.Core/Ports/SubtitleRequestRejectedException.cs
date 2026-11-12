using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleRequestRejectedException : Exception
{
    public SubtitleRequestRejectedException()
        : base("The subtitle request is not valid.")
    {
    }

    public SubtitleRequestRejectedException(string message)
        : base(message)
    {
    }

    public SubtitleRequestRejectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleStaleCandidateException : Exception
{
    public SubtitleStaleCandidateException()
        : base("The selected subtitle candidate is no longer available.")
    {
    }

    public SubtitleStaleCandidateException(string message)
        : base(message)
    {
    }

    public SubtitleStaleCandidateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleReadOnlyLibraryException : Exception
{
    public SubtitleReadOnlyLibraryException()
        : base("Subtitle downloads are disabled for this library.")
    {
    }

    public SubtitleReadOnlyLibraryException(string message)
        : base(message)
    {
    }

    public SubtitleReadOnlyLibraryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

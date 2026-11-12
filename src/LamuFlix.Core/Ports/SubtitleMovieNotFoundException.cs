using System;

namespace LamuFlix.Core.Ports;

public sealed class SubtitleMovieNotFoundException : Exception
{
    public SubtitleMovieNotFoundException()
        : base("The movie is not present in the library.")
    {
    }

    public SubtitleMovieNotFoundException(string message)
        : base(message)
    {
    }

    public SubtitleMovieNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

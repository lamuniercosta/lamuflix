using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record ThumbnailRequested
{
    public ThumbnailRequested(MovieId movieId, string filePath)
    {
        MovieId = movieId;
        FilePath = filePath;
    }

    public MovieId MovieId { get; }

    public string FilePath { get; }
}

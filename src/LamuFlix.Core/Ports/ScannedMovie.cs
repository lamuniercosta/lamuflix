using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record ScannedMovie(LibraryPath Path, string Title, MediaFormat Format)
{
    public override string ToString() => $"{nameof(ScannedMovie)} {{ {nameof(Path)} = {Path}, {nameof(Title)} = {Title}, {nameof(Format)} = {Format} }}";
}

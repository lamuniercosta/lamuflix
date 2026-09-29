using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record MovieDetails(
    MovieId Id,
    string Title,
    LibraryPath Path,
    MediaFormat Format,
    MovieMetadata? Metadata)
{
    public override string ToString() => $"{nameof(MovieDetails)} {{ {nameof(Id)} = {Id}, {nameof(Title)} = {Title}, {nameof(Path)} = {Path}, {nameof(Format)} = {Format}, {nameof(Metadata)} = {Metadata} }}";
}

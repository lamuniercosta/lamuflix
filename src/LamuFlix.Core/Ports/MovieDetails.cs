using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record MovieDetails(
    MovieId Id,
    string Title,
    LibraryPath Path,
    MediaFormat Format,
    MovieMetadata? Metadata);

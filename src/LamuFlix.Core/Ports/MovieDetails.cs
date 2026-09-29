using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

// ReSharper disable NotAccessedPositionalProperty.Global
// Consumed by future movie catalog and database adapters
public sealed record MovieDetails(
    MovieId Id,
    string Title,
    LibraryPath Path,
    MediaFormat Format,
    MovieMetadata? Metadata);
// ReSharper restore NotAccessedPositionalProperty.Global

using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record ScannedMovie(LibraryPath Path, string Title, MediaFormat Format);

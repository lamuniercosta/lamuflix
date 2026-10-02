using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

// ReSharper disable NotAccessedPositionalProperty.Global
// Consumed by future media library scanner and filesystem adapters
public sealed record ScannedMovie(LibraryPath Path, string Title, MediaFormat Format, ReleaseYear? Year, long SizeBytes);
// ReSharper restore NotAccessedPositionalProperty.Global

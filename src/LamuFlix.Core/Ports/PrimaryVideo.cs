using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record PrimaryVideo(LibraryPath Path, MediaFormat Format);

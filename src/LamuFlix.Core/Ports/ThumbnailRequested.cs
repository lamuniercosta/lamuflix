using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public sealed record ThumbnailRequested(MovieId MovieId, string FilePath);
